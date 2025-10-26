## Intro

This solution is meant to address a failure to install Android Build Tools through UnityHub for Unity 6.2 on Windows 10. In a nutshell, the internals of UnityHub use the system's `tar.exe` with flags `-a -x -f` together to unzip the file contents needed for the installation. This seems to throw an error described in specifics found in [this forum post](https://discussions.unity.com/t/unityhub-fails-to-install-android-build-support-for-unity-6-2-9f1/1692116). The workaround is to reroute the call to `tar.exe` to a "wrapper" `tar.exe` in a different directory that strips the `-a` flag from the arguments being passed by UnityHub, and then calls the system's `tar.exe` with only the `-x -f` flags, which seems to resolve the issue.

---

## Steps
1. Install the "wrapper" `tar.exe` from this solution somewhere on your system (e.g. `C:\Tools\bin\tar.exe`)
2. Using PowerShell, add that path as the very first entry to your system's environment PATH.
- 	```powershell
	setx /M PATH "C:\Tools\bin;$($env:PATH)"
	```
- Alternatively, this can also be done through system settings. [See guide here](https://www.wikihow.com/Change-the-PATH-Environment-Variable-on-Windows). Just be sure to move the new entry to the *TOP* position. This is so the "wrapper" `tar.exe` is found first before the one at `System32\tar.exe`
3. Restart UnityHub, and retry the installation of Android Build Tools.

You can use the prebuilt `tar.exe` file found in [Releases](https://github.com/acampos81/unity-6.2-android-tar/releases), or you can build it from source with the following PowerShell command:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o C:/Tools/bin/tar/
```

#### Using Separate Drives For Unity Installs
UnityHub typically downloads temporary zip files to a directory like `C:\Users\<your-username>\AppData\Local\Temp` and then installs them wherever you've configured Unity editor version to go.  For most people that's default location on the `C:` drive. If you've modified the editors install location to somewhere other than the `C:` drive, then you'll need to build from source and modify the regular expression used to isolate the `.zip` path and the destination path.  For example, if you're installing all editor version on the `D:` drive, then you'll need to add your drive to the expression found on line 12 to go from `@"([C:][\w\d\.\:\-\\\/]+)";`  to `@"([CD:][\w\d\.\:\-\\\/]+)";`. Otherwise, paths will not be captured set correctly.