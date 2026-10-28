import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { RtcRole, RtcTokenBuilder } from "npm:agora-token@2.0.3";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

type Body = { action?: string; roomId?: string; requestId?: string };

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
  // The Speaking room is online play: registered accounts only (OnlineAccess in the game), enforced here too.
  if (user.is_anonymous) return reply({ ok: false, error: "Phòng Speaking cần tài khoản đã đăng ký." }, 403);

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
    const { data: existing } = await admin.from("speaking_rooms").select("*")
      .eq("created_by", user.id).in("status", ["waiting", "matched"]).maybeSingle();
    if (existing) return reply({ ok: true, role, room: existing });
    const { data: profile } = await admin.from("profiles").select("display_name,avatar_url,bio,level,xp").eq("id", user.id).maybeSingle();
    const channel = `nl_${crypto.randomUUID().replaceAll("-", "")}`;
    const { data, error } = await admin.from("speaking_rooms").insert({
      created_by: user.id, creator_role: teacher ? "teacher" : "student",
      student_id: teacher ? null : user.id,
      student_name: teacher ? "" : String(profile?.display_name ?? displayName).slice(0, 80),
      student_avatar_url: teacher ? null : profile?.avatar_url ?? null,
      student_bio: teacher ? null : profile?.bio ?? null,
      student_level: teacher ? 1 : profile?.level ?? 1,
      student_xp: teacher ? 0 : profile?.xp ?? 0,
      teacher_id: teacher ? user.id : null,
      teacher_name: teacher ? displayName : null,
      channel_name: channel,
    }).select("*").single();
    if (error) return reply({ ok: false, error: error.message }, 400);
    return reply({ ok: true, role, room: data });
  }

  if (body.action === "list") {
    const wantedRole = teacher ? "student" : "teacher";
    const { data, error } = await admin.from("speaking_rooms").select("*").eq("status", "waiting").eq("creator_role", wantedRole)
      .order("created_at", { ascending: true }).limit(30);
    const rooms = (data ?? []).map((room) => ({
      ...room, avatar_url: room.student_avatar_url, bio: room.student_bio,
      level: room.student_level, xp: room.student_xp,
    }));
    return error ? reply({ ok: false, error: error.message }, 400) : reply({ ok: true, role, rooms });
  }

  if (body.action === "mine") {
    const query = admin.from("speaking_rooms").select("*").in("status", ["waiting", "matched"]).order("created_at", { ascending: false }).limit(1);
    const { data, error } = teacher ? await query.eq("teacher_id", user.id) : await query.or(`student_id.eq.${user.id},created_by.eq.${user.id}`);
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

  if (body.action === "request_join") {
    if (teacher) return reply({ ok: false, error: "Giáo viên không gửi yêu cầu học viên." }, 403);
    if (!body.roomId) return reply({ ok: false, error: "Thiếu roomId." }, 400);
    const { data: room } = await admin.from("speaking_rooms").select("*").eq("id", body.roomId).eq("status", "waiting").eq("creator_role", "teacher").maybeSingle();
    if (!room) return reply({ ok: false, error: "Phòng giáo viên không còn mở." }, 404);
    const { data: profile } = await admin.from("profiles").select("display_name,avatar_url,bio,level,xp").eq("id", user.id).maybeSingle();
    const { error } = await admin.from("speaking_room_requests").upsert({
      room_id: room.id, student_id: user.id, student_name: profile?.display_name ?? displayName,
      student_avatar_url: profile?.avatar_url ?? null, student_bio: profile?.bio ?? null,
      student_level: profile?.level ?? 1, student_xp: profile?.xp ?? 0, status: "pending",
    }, { onConflict: "room_id,student_id" });
    return error ? reply({ ok: false, error: error.message }, 400) : reply({ ok: true, role, room });
  }

  if (body.action === "requests") {
    if (!teacher) return reply({ ok: false, error: "Chỉ giáo viên được duyệt yêu cầu." }, 403);
    const { data: rooms } = await admin.from("speaking_rooms").select("id").eq("teacher_id", user.id).eq("status", "waiting");
    const ids = (rooms ?? []).map((r) => r.id);
    if (ids.length === 0) return reply({ ok: true, role, requests: [] });
    const { data, error } = await admin.from("speaking_room_requests").select("*").in("room_id", ids).eq("status", "pending").order("created_at");
    const requests = (data ?? []).map((item) => ({ ...item, avatar_url: item.student_avatar_url, bio: item.student_bio, level: item.student_level, xp: item.student_xp }));
    return error ? reply({ ok: false, error: error.message }, 400) : reply({ ok: true, role, requests });
  }

  if (body.action === "approve") {
    if (!teacher || !body.requestId) return reply({ ok: false, error: "Yêu cầu không hợp lệ." }, 403);
    const { data: joinRequest } = await admin.from("speaking_room_requests").select("*").eq("id", body.requestId).eq("status", "pending").maybeSingle();
    if (!joinRequest) return reply({ ok: false, error: "Yêu cầu đã được xử lý." }, 409);
    const { data: room } = await admin.from("speaking_rooms").update({
      student_id: joinRequest.student_id, student_name: joinRequest.student_name,
      student_avatar_url: joinRequest.student_avatar_url, student_bio: joinRequest.student_bio,
      student_level: joinRequest.student_level, student_xp: joinRequest.student_xp,
      status: "matched", matched_at: new Date().toISOString(),
    }).eq("id", joinRequest.room_id).eq("teacher_id", user.id).eq("status", "waiting").select("*").maybeSingle();
    if (!room) return reply({ ok: false, error: "Phòng không còn nhận học viên." }, 409);
    await admin.from("speaking_room_requests").update({ status: "accepted" }).eq("id", joinRequest.id);
    await admin.from("speaking_room_requests").update({ status: "rejected" }).eq("room_id", room.id).neq("id", joinRequest.id).eq("status", "pending");
    return issueToken(room, user.id, role);
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

