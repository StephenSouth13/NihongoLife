import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { RtcRole, RtcTokenBuilder } from "npm:agora-token@2.0.3";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

type Body = { action?: string; roomId?: string };

function reply(body: Record<string, unknown>, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...cors, "Content-Type": "application/json" },
  });
}

function agoraUid(userId: string): number {
  // Stable non-zero uint32; the room membership is still authorized by Supabase user id.
  let hash = 2166136261;
  for (let i = 0; i < userId.length; i++) hash = Math.imul(hash ^ userId.charCodeAt(i), 16777619);
  return (hash >>> 0) || 1;
}

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return reply({ ok: false, error: "Method not allowed" }, 405);

  const url = Deno.env.get("SUPABASE_URL")!;
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY")!;
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
  const authorization = request.headers.get("Authorization") ?? "";
  const authClient = createClient(url, anonKey, { global: { headers: { Authorization: authorization } } });
  const { data: { user }, error: authError } = await authClient.auth.getUser();
  if (authError || !user) return reply({ ok: false, error: "Phiên đăng nhập không hợp lệ." }, 401);

  const admin = createClient(url, serviceKey);
  const body = await request.json().catch(() => ({})) as Body;
  const role = String(user.app_metadata?.role ?? "student").toLowerCase();
  const teacher = role === "teacher" || role === "admin";
  const displayName = String(user.user_metadata?.display_name ?? (teacher ? "Giáo viên" : "Học viên")).slice(0, 80);

  // Do not leave abandoned waiting rooms visible forever.
  const staleBefore = new Date(Date.now() - 30 * 60 * 1000).toISOString();
  await admin.from("speaking_rooms").update({ status: "closed", closed_at: new Date().toISOString() })
    .eq("status", "waiting").lt("created_at", staleBefore);

  if (body.action === "role") return reply({ ok: true, role });

  if (body.action === "create") {
    if (teacher) return reply({ ok: false, error: "Tài khoản giáo viên không tạo phòng chờ học viên." }, 403);
    const { data: existing } = await admin.from("speaking_rooms").select("*")
      .eq("student_id", user.id).in("status", ["waiting", "matched"]).maybeSingle();
    if (existing) return reply({ ok: true, role, room: existing });
    const { data: profile } = await admin.from("profiles").select("display_name,avatar_url,bio,level,xp").eq("id", user.id).maybeSingle();
    const channel = `nl_${crypto.randomUUID().replaceAll("-", "")}`;
    const { data, error } = await admin.from("speaking_rooms").insert({
      student_id: user.id,
      student_name: String(profile?.display_name ?? displayName).slice(0, 80),
      student_avatar_url: profile?.avatar_url ?? null,
      student_bio: profile?.bio ?? null,
      student_level: profile?.level ?? 1,
      student_xp: profile?.xp ?? 0,
      channel_name: channel,
    }).select("*").single();
    if (error) return reply({ ok: false, error: error.message }, 400);
    return reply({ ok: true, role, room: data });
  }

  if (body.action === "list") {
    if (!teacher) return reply({ ok: false, error: "Chỉ giáo viên được xem danh sách phòng chờ." }, 403);
    const { data, error } = await admin.from("speaking_rooms").select("*").eq("status", "waiting")
      .order("created_at", { ascending: true }).limit(30);
    const rooms = (data ?? []).map((room) => ({
      ...room, avatar_url: room.student_avatar_url, bio: room.student_bio,
      level: room.student_level, xp: room.student_xp,
    }));
    return error ? reply({ ok: false, error: error.message }, 400) : reply({ ok: true, role, rooms });
  }

  if (body.action === "mine") {
    const query = admin.from("speaking_rooms").select("*").in("status", ["waiting", "matched"])
      .order("created_at", { ascending: false }).limit(1);
    const { data, error } = teacher ? await query.eq("teacher_id", user.id) : await query.eq("student_id", user.id);
    return error ? reply({ ok: false, error: error.message }, 400) : reply({ ok: true, role, room: data?.[0] ?? null });
  }

  if (body.action === "claim") {
    if (!teacher) return reply({ ok: false, error: "Chỉ giáo viên được nhận lớp." }, 403);
    if (!body.roomId) return reply({ ok: false, error: "Thiếu roomId." }, 400);
    const { data, error } = await admin.from("speaking_rooms").update({
      teacher_id: user.id, teacher_name: displayName, status: "matched", matched_at: new Date().toISOString(),
    }).eq("id", body.roomId).eq("status", "waiting").select("*").maybeSingle();
    if (error || !data) return reply({ ok: false, error: error?.message ?? "Phòng đã được giáo viên khác nhận." }, 409);
    return issueToken(data, user.id, role);
  }

  if (body.action === "token") {
    if (!body.roomId) return reply({ ok: false, error: "Thiếu roomId." }, 400);
    const { data, error } = await admin.from("speaking_rooms").select("*").eq("id", body.roomId).eq("status", "matched").maybeSingle();
    if (error || !data) return reply({ ok: false, error: "Phòng chưa được ghép hoặc không tồn tại." }, 404);
    if (data.student_id !== user.id && data.teacher_id !== user.id)
      return reply({ ok: false, error: "Bạn không thuộc phòng này." }, 403);
    return issueToken(data, user.id, role);
  }

  if (body.action === "close") {
    if (!body.roomId) return reply({ ok: false, error: "Thiếu roomId." }, 400);
    let close = admin.from("speaking_rooms").update({ status: "closed", closed_at: new Date().toISOString() }).eq("id", body.roomId);
    close = teacher ? close.eq("teacher_id", user.id) : close.eq("student_id", user.id);
    const { error } = await close;
    return error ? reply({ ok: false, error: error.message }, 400) : reply({ ok: true, role });
  }

  return reply({ ok: false, error: "Action không hợp lệ." }, 400);
});

function issueToken(room: Record<string, unknown>, userId: string, role: string) {
  const appId = Deno.env.get("AGORA_APP_ID") ?? "";
  const certificate = Deno.env.get("AGORA_APP_CERTIFICATE") ?? "";
  if (!appId || !certificate) return reply({ ok: false, error: "Server chưa cấu hình Agora App ID/Certificate." }, 503);
  const uid = agoraUid(userId);
  const expiresIn = 3600;
  const expiresAt = Math.floor(Date.now() / 1000) + expiresIn;
  const channelName = String(room.channel_name);
  const token = RtcTokenBuilder.buildTokenWithUid(appId, certificate, channelName, uid, RtcRole.PUBLISHER, expiresAt);
  return reply({ ok: true, role, room, token, channelName, uid, expiresIn });
}

