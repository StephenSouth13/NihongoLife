using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// End-to-end encryption for private messages. Each install has an RSA-2048 key pair (kept in PlayerPrefs, the
    /// private half never leaves the machine). A message is sealed with a fresh AES-256-CBC key and HMAC-SHA256
    /// (encrypt-then-MAC); that 64-byte key pair is wrapped with the recipient's public key (RSA-OAEP) and the whole
    /// envelope is signed with the sender's private key (RSA-SHA256). The server and other players only ever see
    /// ciphertext; a forged or altered message fails the signature and is dropped.
    ///
    /// Keys are exchanged on first contact and pinned (trust on first use): if a contact's key later changes, the
    /// chat says so and shows the new safety code.
    /// </summary>
    public sealed class ChatCrypto
    {
        private const string PrefsKey = "NL.Chat.Rsa.v1";
        private RSA _rsa;
        private Task _keyTask;

        public bool Ready => _rsa != null;
        public string PublicKey { get; private set; }
        public string SafetyCode => SafetyCodeOf(PublicKey);

        /// <summary>Loads the saved key pair or makes one on a worker thread (2048-bit generation takes ~1 s).</summary>
        public void EnsureKeys(string prefsSuffix = "")
        {
            if (_rsa != null || _keyTask != null) return;
            string saved = PlayerPrefs.GetString(PrefsKey + prefsSuffix, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                try { Adopt(Import(saved, true)); return; }
                catch (Exception ex) { Debug.LogWarning($"[ChatCrypto] Saved key unreadable, making a new one: {ex.Message}"); }
            }
            _keyTask = Task.Run(() =>
            {
                var rsa = RSA.Create();
                rsa.KeySize = 2048;
                rsa.ExportParameters(true); // forces generation off the main thread
                return rsa;
            }).ContinueWith(t =>
            {
                if (t.IsFaulted) { Debug.LogError($"[ChatCrypto] Key generation failed: {t.Exception?.GetBaseException().Message}"); return; }
                _pendingSave = (t.Result, prefsSuffix);
            });
        }

        private (RSA rsa, string suffix)? _pendingSave;

        /// <summary>Main thread: finish a background key generation (PlayerPrefs is main-thread only).</summary>
        public void Pump()
        {
            if (_pendingSave == null) return;
            var (rsa, suffix) = _pendingSave.Value;
            _pendingSave = null;
            PlayerPrefs.SetString(PrefsKey + suffix, Export(rsa.ExportParameters(true), true));
            PlayerPrefs.Save();
            Adopt(rsa);
        }

        private void Adopt(RSA rsa)
        {
            _rsa = rsa;
            PublicKey = Export(rsa.ExportParameters(false), false);
        }

        // ─────────── Seal / open ───────────

        public Dictionary<string, object> Seal(string plainText, string recipientPublicKey)
        {
            if (!Ready) throw new InvalidOperationException("Keys not ready");
            byte[] keys = RandomBytes(64);
            byte[] iv = RandomBytes(16);
            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = Slice(keys, 0, 32);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using var enc = aes.CreateEncryptor();
                byte[] plain = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
                cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
            }
            byte[] mac;
            using (var hmac = new HMACSHA256(Slice(keys, 32, 32))) mac = hmac.ComputeHash(Concat(iv, cipher));
            byte[] wrapped;
            using (var peer = Import(recipientPublicKey, false)) wrapped = peer.Encrypt(keys, RSAEncryptionPadding.OaepSHA1);
            byte[] signature = _rsa.SignData(Concat(iv, cipher, mac, wrapped), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return new Dictionary<string, object>
            {
                ["iv"] = Convert.ToBase64String(iv),
                ["ct"] = Convert.ToBase64String(cipher),
                ["mac"] = Convert.ToBase64String(mac),
                ["ek"] = Convert.ToBase64String(wrapped),
                ["sig"] = Convert.ToBase64String(signature)
            };
        }

        /// <summary>Returns the text, or null when the envelope is forged, altered or not for us.</summary>
        public string Open(Dictionary<string, object> envelope, string senderPublicKey)
        {
            if (!Ready || envelope == null || string.IsNullOrEmpty(senderPublicKey)) return null;
            try
            {
                byte[] iv = Convert.FromBase64String(envelope.Str("iv", ""));
                byte[] cipher = Convert.FromBase64String(envelope.Str("ct", ""));
                byte[] mac = Convert.FromBase64String(envelope.Str("mac", ""));
                byte[] wrapped = Convert.FromBase64String(envelope.Str("ek", ""));
                byte[] signature = Convert.FromBase64String(envelope.Str("sig", ""));
                using (var sender = Import(senderPublicKey, false))
                {
                    if (!sender.VerifyData(Concat(iv, cipher, mac, wrapped), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) return null;
                }
                byte[] keys = _rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA1);
                if (keys.Length != 64) return null;
                using (var hmac = new HMACSHA256(Slice(keys, 32, 32)))
                {
                    if (!FixedEquals(hmac.ComputeHash(Concat(iv, cipher)), mac)) return null;
                }
                using var aes = Aes.Create();
                aes.Key = Slice(keys, 0, 32);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using var dec = aes.CreateDecryptor();
                return Encoding.UTF8.GetString(dec.TransformFinalBlock(cipher, 0, cipher.Length));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Short code both people can compare out loud to be sure no one sits in the middle.</summary>
        public static string SafetyCodeOf(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey)) return "—";
            using var sha = SHA256.Create();
            byte[] h = sha.ComputeHash(Encoding.ASCII.GetBytes(publicKey));
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                if (i > 0 && i % 2 == 0) sb.Append('-');
                sb.Append(h[i].ToString("X2"));
            }
            return sb.ToString();
        }

        // ─────────── Encoding ───────────

        private static string Export(RSAParameters p, bool withPrivate)
        {
            string B(byte[] b) => b == null ? "" : Convert.ToBase64String(b);
            string pub = B(p.Modulus) + "." + B(p.Exponent);
            return withPrivate ? pub + "." + B(p.D) + "." + B(p.P) + "." + B(p.Q) + "." + B(p.DP) + "." + B(p.DQ) + "." + B(p.InverseQ) : pub;
        }

        private static RSA Import(string text, bool withPrivate)
        {
            string[] parts = text.Split('.');
            byte[] F(int i) => Convert.FromBase64String(parts[i]);
            var p = new RSAParameters { Modulus = F(0), Exponent = F(1) };
            if (withPrivate)
            {
                p.D = F(2); p.P = F(3); p.Q = F(4); p.DP = F(5); p.DQ = F(6); p.InverseQ = F(7);
            }
            var rsa = RSA.Create();
            rsa.ImportParameters(p);
            return rsa;
        }

        public static bool IsValidPublicKey(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 1024) return false;
            try { using var rsa = Import(text, false); return rsa.KeySize >= 2048; }
            catch (Exception) { return false; }
        }

        private static byte[] RandomBytes(int n)
        {
            var b = new byte[n];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(b);
            return b;
        }

        private static byte[] Slice(byte[] b, int start, int length)
        {
            var r = new byte[length];
            Buffer.BlockCopy(b, start, r, 0, length);
            return r;
        }

        private static byte[] Concat(params byte[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var r = new byte[n];
            int o = 0;
            foreach (var p in parts) { Buffer.BlockCopy(p, 0, r, o, p.Length); o += p.Length; }
            return r;
        }

        private static bool FixedEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
