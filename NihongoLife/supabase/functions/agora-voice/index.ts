import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { RtcRole, RtcTokenBuilder } from "npm:agora-token@2.0.3";

// Player voice rooms for NihongoLife (area / group / private call). Separate from agora-classroom on purpose:
// these rooms need no teacher approval, but every caller must be a signed-in (or anonymous) Supabase user and
// the room name must be one of the game's patterns. The App Certificate never leaves the server.

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

// nlv_<scene>_<shard>  area voice of a world shard
// nlg_<groupId>         a chat group
// nld_<16 hex>          a private call between two players
const CHANNEL = /^(nlv_[a-z0-9_]{1,56}|nlg_[a-z0-9]{4,32}|nld_[0-9a-f]{16})$/;

function reply(body: Record<string, unknown>, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
}

function agoraUid(userId: string): number {
  // Same FNV-1a as agora-classroom and VoiceChatService.AgoraUid, so clients can map a player id to a voice uid.
  let hash = 2166136261;
  for (let i = 0; i < userId.length; i++) hash = Math.imul(hash ^ userId.charCodeAt(i), 16777619);
  return (hash >>> 0) || 1;
}

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return reply({ ok: false, error: "Method not allowed" }, 405);

  const url = Deno.env.get("SUPABASE_URL")!;
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY")!;
  const authClient = createClient(url, anonKey, { global: { headers: { Authorization: request.headers.get("Authorization") ?? "" } } });
  const { data: { user }, error } = await authClient.auth.getUser();
  if (error || !user) return reply({ ok: false, error: "Cần đăng nhập để dùng voice." }, 401);

  const body = await request.json().catch(() => ({})) as { channel?: string };
  const channel = String(body.channel ?? "");
  if (!CHANNEL.test(channel)) return reply({ ok: false, error: "Tên phòng voice không hợp lệ." }, 400);

  const appId = Deno.env.get("AGORA_APP_ID") ?? "";
  const certificate = Deno.env.get("AGORA_APP_CERTIFICATE") ?? "";
  if (!appId || !certificate) return reply({ ok: false, error: "Server chưa cấu hình Agora App ID/Certificate." }, 503);

  const uid = agoraUid(user.id);
  const expiresIn = 3600;
  const expiresAt = Math.floor(Date.now() / 1000) + expiresIn;
  // Publisher privilege so push-to-talk can switch to speaker; the client stays audience until the key is held.
  const token = RtcTokenBuilder.buildTokenWithUid(appId, certificate, channel, uid, RtcRole.PUBLISHER, expiresAt);
  return reply({ ok: true, token, channel, uid, expiresIn });
});
