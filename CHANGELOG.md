# Changelog

All notable changes to this project will be documented in this file

## 0.4.2 - 2025-05-26

- Fix loading of custom icon shaders with KSPCF v1.37+ installed


## 0.4.1 - 2025-04-04

- Gracefully handle failing to patch a Shader.Find callsite (usually in generic methods).  Specifically this would occur with KRPC and cause any further patches to be skipped.
- Change several spammy log messages to be debug-only
- Skip applying material replacements to renderers that don't have a sharedMaterial (some ParticleSystemRenderers apparently)


## 0.4.0 - 2024-09-09

Major build system refactor, and adoption by the KSPModdingLibs team.

### Added

- Added material-replacement functionality using the `SHABBY_MATERIAL_DEF` node


## 0.3.0 - 2022-08-03

Add the rest of the main harmony distro


## 0.2.0 - 2021-03-18

Migrate to Harmony 2.0


## 0.1.2 - 2020-06-15

Make the zip have GameData as the root, As requested by @drewcassidy (CineboxAndrew).


## 0.1.1 - 2020-06-13

### Removed

- Remove KSP version specification. Either it works or it doesn't.


## 0.1.0 - 2020-06-13

### Changed

- Make the logging a little less noisy
    - Shows only custom shaders being found and a (hopefully) successful hook.


## 0.0.0 - 2020-05-09

initial commit

- Shaders are loaded from asset bundles with a ".shab" extension.
