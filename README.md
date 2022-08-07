Shabby Shader Loader for KSP
===

### Building

1. Populate the Source/dlls folder with the dlls from your KSP install, as well as the `0harmony.dll` file from KSPHarmony
2. generate versioned file info with `python tools/version.py`. you may need to run `pip install -r tools/requirements.txt` first
3. build dll with `dotnet build Shabby.sln`

### Usage

Generate an assetbundle with the shaders you want to load, and name it with the `.shab` file extension.