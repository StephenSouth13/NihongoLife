using System;

namespace NihongoLife.Core
{
    public enum AuthState
    {
        SignedOut,
        SignedIn,
        TokenRefreshed,
        SessionExpired
    }

    /// <summary>
    /// Interface for authentication services.
    /// Allows swapping between Supabase auth and local/mock auth.
    /// </summary>
    public interface IAuthService : IGameService
    {
        /// <summary>Whether the user is currently authenticated.</summary>
        bool IsAuthenticated { get; }

        /// <summary>The unique user ID from Supabase Auth.</summary>
        string UserId { get; }

        /// <summary>The current access token (JWT).</summary>
        string AccessToken { get; }

        /// <summary>The user's display name.</summary>
        string DisplayName { get; }

        /// <summary>The user's email (may be empty for anonymous users).</summary>
        string Email { get; }

        /// <summary>Fired when auth state changes (sign in, sign out, token refresh).</summary>
        event Action<AuthState> OnAuthStateChanged;

        /// <summary>Sign in without creating an account. Generates a temporary anonymous session.</summary>
        void SignInAnonymously(Action<bool, string> callback);

        /// <summary>Sign in with email and password.</summary>
        void SignInWithEmail(string email, string password, Action<bool, string> callback);

        /// <summary>Create a new account with email, password, and display name.</summary>
        void SignUpWithEmail(string email, string password, string displayName, Action<bool, string> callback);

        /// <summary>Sign out and clear the session.</summary>
        void SignOut();

        /// <summary>Attempt to restore a previous session from local storage.</summary>
        void TryRestoreSession(Action<bool> callback);
    }
}
