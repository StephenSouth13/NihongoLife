"""
Load test for the NihongoLife online world (Supabase Realtime), same protocol as SupabaseRealtimeClient /
SupabaseOnlineWorldService: N bots join one world shard, track presence, and send poses the way the game does
(~6 Hz while walking, every 3 s while standing; about a third of players walk at any moment).

Measures per stage: delivery ratio, end-to-end latency (p50/p95/max), server errors, and the number of Realtime
messages used (each broadcast counts once sent + once per receiver — keep stages short on the free plan).

Usage:  python -I online_load_test.py [--stages 20,40,60] [--seconds 15]
Reads the project URL and anon key from Resources/Control/NihongoLifeControlDatabase.asset (never the service key).
"""
import argparse
import asyncio
import json
import random
import re
import statistics
import time
from pathlib import Path

import websockets

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / "Assets/NihongoLife/Resources/Control/NihongoLifeControlDatabase.asset"


def config():
    text = ASSET.read_text(encoding="utf-8")
    url = re.search(r"supabaseProjectUrl: (\S+)", text).group(1).rstrip("/")
    key = re.search(r"supabaseAnonKey: (\S+)", text).group(1)
    return url.replace("https://", "wss://") + f"/realtime/v1/websocket?apikey={key}&vsn=1.0.0", key


class Bot:
    def __init__(self, index, topic, url, key, stats, shard_size=1):
        # Same rule as SupabaseOnlineWorldService.MovingSendInterval: 6 Hz alone, slower as the shard fills.
        self.moving_interval = min(max(1 / 6 * max(1, shard_size / 10), 1 / 6), 0.5)
        self.backoff_until, self.backoff = 0.0, 1.0
        self.id = f"loadbot_{index}"
        self.topic = "realtime:" + topic
        self.url, self.key, self.stats = url, key, stats
        self.ref = 0
        self.pos = [random.uniform(-20, 20), 0.0, random.uniform(-20, 20)]
        self.moving = random.random() < 0.35
        self.ws = None

    def next_ref(self):
        self.ref += 1
        return str(self.ref)

    async def send(self, event, payload):
        await self.ws.send(json.dumps({"topic": self.topic, "event": event, "payload": payload, "ref": self.next_ref(), "join_ref": "1"}))

    async def run(self, seconds, start_barrier, delay=0.0):
        await asyncio.sleep(delay)  # players do not all connect in the same millisecond
        try:
            self.ws = await websockets.connect(self.url, max_queue=None, ping_interval=20)
        except Exception as e:
            self.stats["connect_errors"] += 1
            self.stats["error_samples"].append(f"connect: {e}")
            return
        await self.ws.send(json.dumps({"topic": self.topic, "event": "phx_join", "ref": "1", "join_ref": "1", "payload": {
            "config": {"broadcast": {"self": False, "ack": False}, "presence": {"key": self.id, "enabled": True}, "postgres_changes": [], "private": False},
            "access_token": self.key}}))
        reader = asyncio.create_task(self.read())
        await start_barrier.wait()
        await self.send("presence", {"type": "presence", "event": "track", "payload": {"n": self.id, "c": "kid_haruto"}})
        end = time.time() + seconds
        next_toggle = time.time() + random.uniform(2, 4)
        while time.time() < end:
            now = time.time()
            if now >= next_toggle:
                self.moving = random.random() < 0.35
                next_toggle = now + random.uniform(2, 4)
            if self.moving:
                self.pos[0] += random.uniform(-0.6, 0.6)
                self.pos[2] += random.uniform(-0.6, 0.6)
            payload = {"i": self.id, "x": int(self.pos[0] * 100), "y": 0, "z": int(self.pos[2] * 100),
                       "r": random.randint(0, 359), "m": 1 if self.moving else 0, "ts": int(time.time() * 1000)}
            try:
                await self.send("broadcast", {"type": "broadcast", "event": "p", "payload": payload})
                self.stats["sent"] += 1
            except Exception as e:
                self.stats["send_errors"] += 1
                self.stats["error_samples"].append(f"send: {e}")
                break
            interval = self.moving_interval * (self.backoff if time.time() < self.backoff_until else 1.0)
            await asyncio.sleep(interval if self.moving else 3.0)
        await asyncio.sleep(2.0)  # let the last messages arrive
        reader.cancel()
        try:
            await self.ws.close()
        except Exception:
            pass

    async def read(self):
        try:
            async for raw in self.ws:
                msg = json.loads(raw)
                ev = msg.get("event")
                payload = msg.get("payload") or {}
                if ev == "broadcast" and payload.get("event") == "p":
                    self.stats["received"] += 1
                    ts = (payload.get("payload") or {}).get("ts")
                    if ts:
                        self.stats["latency"].append(time.time() * 1000 - ts)
                elif ev in ("presence_diff", "presence_state"):
                    self.stats["presence"] += 1
                elif ev == "phx_reply" and payload.get("status") != "ok":
                    self.stats["server_errors"] += 1
                    self.stats["error_samples"].append(json.dumps(payload)[:200])
                elif ev == "system" and "Too many" in str(payload.get("message")):
                    self.stats["rate_limited"] += 1
                    self.backoff = min(self.backoff * 2, 4.0) if time.time() < self.backoff_until else 2.0
                    self.backoff_until = time.time() + 20
                elif ev in ("phx_error", "phx_close") or (ev == "system" and payload.get("status") == "error"):
                    self.stats["server_errors"] += 1
                    self.stats["error_samples"].append(f"{ev}: {json.dumps(payload)[:200]}")
        except asyncio.CancelledError:
            pass
        except Exception as e:
            self.stats["error_samples"].append(f"read: {e}")


async def stage(n, seconds, url, key):
    topic = f"w:loadtest_{int(time.time())}_{n}:0"
    stats = {"sent": 0, "received": 0, "latency": [], "presence": 0, "server_errors": 0, "rate_limited": 0, "connect_errors": 0, "send_errors": 0, "error_samples": []}
    barrier = asyncio.Event()
    bots = [Bot(i, topic, url, key, stats, n) for i in range(n)]
    tasks = [asyncio.create_task(b.run(seconds, barrier, i * 0.05)) for i, b in enumerate(bots)]
    await asyncio.sleep(2.5 + n * 0.05)  # everyone connected and joined
    barrier.set()
    await asyncio.gather(*tasks)
    expected = stats["sent"] * (n - 1)
    lat = sorted(stats["latency"])
    used = stats["sent"] + stats["received"] + stats["presence"]
    result = {
        "bots": n, "seconds": seconds, "poses_sent": stats["sent"], "sent_per_sec": round(stats["sent"] / seconds, 1),
        "expected_deliveries": expected, "delivered": stats["received"],
        "delivery_pct": round(100 * stats["received"] / expected, 2) if expected else 0,
        "latency_ms_p50": round(statistics.median(lat)) if lat else None,
        "latency_ms_p95": round(lat[int(len(lat) * 0.95) - 1]) if lat else None,
        "latency_ms_max": round(lat[-1]) if lat else None,
        "rate_limit_notices": stats["rate_limited"], "server_errors": stats["server_errors"], "connect_errors": stats["connect_errors"], "send_errors": stats["send_errors"],
        "realtime_messages_used": used,
        "error_samples": stats["error_samples"][:5],
    }
    return result


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--stages", default="20,40,60")
    parser.add_argument("--seconds", type=int, default=15)
    args = parser.parse_args()
    url, key = config()
    results = []
    for n in [int(x) for x in args.stages.split(",")]:
        n = min(n, 60)  # agreed ceiling
        r = await stage(n, args.seconds, url, key)
        results.append(r)
        print(json.dumps(r, ensure_ascii=False))
        await asyncio.sleep(3)
    print("TOTAL realtime messages used:", sum(r["realtime_messages_used"] for r in results))


if __name__ == "__main__":
    asyncio.run(main())
