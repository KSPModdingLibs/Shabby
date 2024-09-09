Shabby
======

[![GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![CKAN: Indexed](https://img.shields.io/badge/CKAN-Indexed-brightgreen.svg)](https://github.com/KSP-CKAN/CKAN)

Shabby is a shader asset bundle loader and material replacement mod for KSP. 

## Dependencies
* [Harmony2](https://github.com/KSPModdingLibs/HarmonyKSP)

## Usage

### Loading Shaders

Create an asset bundle in Unity with the ".shab" file extension. Target Windows, but make OpenGLCore is enabled as a graphics API under Project Settings>Player.

Place the resulting shab file in your mod anywhere under GameData. The shaders within it can be used in mu files just like any of the builtin KSP shaders.

### Replacing Shaders

The `SHABBY` top-level config node contains shader information. with the following sub-nodes:

- **`REPLACEMENT`**, with the string-valued keys `name` and `shader`. Defines a simple one-to-one shader replacement.

- **`ICON_SHADER`**, with the string-valued keys `shader` and `iconShader`. It specifies that the shader with the given name is to be substituted when used in an icon prefab. **Without this configuration, all custom shaders will be replaced by stock code with `KSP/ScreenSpaceMask` in icon prefabs and thus editor part icons.**

The **`SHABBY_MATERIAL_DEF`** top-level config node specifies a replacement shader and any number of shader properties to be modified. It contains the following items:
- **`name`** (required): a unique identifier for this material.
- `displayName` (optional): a human-facing name. Not used in Shabby but may be accessed by other mods depending on this mod. Defaults to `name` if not specified.
- `updateExisting` (optional): whether to apply changes to the existing material, or to create a new material from scratch. Defaults to `true`.
- **`shader`** (optional in update-existing mode, otherwise required): name of the shader to apply. May be a stock shader or one loaded by Shabby.
- `preserveRenderQueue` (optional): whether the existing render queue of the material should be preserved when its shader is replaced. Defaults to `false`, which will reset the render queue to the shader's default.
- One or none of each the following nodes, to specify the corresponding type of shader property to be applied. They contain any number of keys of the format `_Property = value`:
  - `KEYWORD {}`: the value is a boolean (`true` or `false`).
  - `FLOAT {}`
  - `COLOR {}`: the value is either a float color (`r, g, b` or `r, g, b, a` normalized to `[0, 1]`), an HTML hex color (`#rgb`, `#rrggbb`, `#rgba`, or `#rrggbbaa`), or a named Unity color.
  - `VECTOR {}`: the value is a Vector4. All four components must be specified.
  - `TEXTURE {}`: the value is a GameData-relative path to a texture file, sans extension.

Material replacements are applied in `PART`s using the **`SHABBY_MATERIAL_REPLACE`** node. It contains the following items:
- **`materialDef`** (required): the unique identifier of the material definition to apply.
- Optionally, exactly one of the following. If neither are specified, the replacement is applied to the entire part.
  1. At least one `targetMaterial` key with a string value, specifying that the existing material with that name is to be replaced. This is the recommended workflow.
  2. At least one `targetTransform` key with a string value, specifying that all meshes under that transform (recursively) are to have their materials replaced.
- Optionally, any number of `ignoreMesh` keys with string values, specifying that the mesh with the given name is to be ignored even if it matches one of the target conditions. This applies to the mesh itself only, not any of its children.

Multiple material replacement nodes may be defined in the part, but they should apply to distinct meshes. The behavior in case of overlap is unspecified.

All of the above configurations may be modified by ModuleManager.

The material replacement is performed once per part, during prefab compilation. The result is indistinguishable from the model `mu` natively containing the replaced material. In particular, this is compatible with existing texture switching mods such as B9PartSwitch.

An example configuration:

```text
SHABBY_MATERIAL_DEF
{
    name = ExampleMaterial

    shader = TU/Metallic  // This is just an example; to use a TU shader one must load it as a `.shab` bundle first.

    Texture
    {
        _MainTex = MyMod/Assets/PBRdiff
        _MetallicGlossMap = MyMod/Assets/PBRmet
        _AOMap = MyMod/Assets/PBRmet
    }

    Float
    {
        _Metal = 0.5
        _Smoothness = 0.96
    }
}

@PART[MyPart]:FOR[MyMod]
{
    SHABBY_MATERIAL_REPLACE
    {
        materialDef = ExampleMaterial
        targetTransform = transformA
        targetTransform = transformB
        excludeMesh = Flag
    }
}
```



