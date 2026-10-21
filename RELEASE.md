# NihongoLife release builds

Release builds are non-interactive and use only the production scenes declared in
`ReleaseBuild.cs`. The sandbox and capture scenes are never included.

## Validate

```powershell
.\build-release.ps1 -Target validate -Version 1.0.0 -AndroidCode 1
```

## Website (WebGL)

```powershell
.\build-release.ps1 -Target web -Version 1.0.0 -AndroidCode 1
```

Upload the contents of `Build/Release/WebGL` to an HTTPS host. The host must serve
`.wasm` as `application/wasm` and preserve the `Content-Encoding: br` header for
Brotli-compressed build files.

## Google Play

Configure the release keystore in the CI/store environment, then run:

```powershell
.\build-release.ps1 -Target android -Version 1.0.0 -AndroidCode 1
```

The Play-ready App Bundle is written to `Build/Release/Android/NihongoLife.aab`.
Increment `AndroidCode` for every upload. Never commit a keystore or its passwords.

## Apple App Store

iOS uses the same identifier (`com.vtcacademy.nihongolife`) and has camera/microphone
privacy text configured. Install iOS Build Support for Unity 6000.3.12f1 on a Mac,
export the Xcode project there, then sign/archive it with the Apple Developer team.
The Windows workstation cannot produce or sign the final App Store archive.

## Required release checks

- Activate a valid Unity Editor license on the build machine.
- Supply production Supabase/Agora/UGS configuration outside source control.
- Run Edit Mode and Play Mode tests, then test every production scene in Play Mode.
- Verify keyboard/mouse in WebGL and touch controls on physical Android/iOS devices.
- Verify microphone/camera permission denial and recovery.
- Complete store privacy/data-safety declarations and provide screenshots/icons.
