# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.1] - 2020-12-26
### Fixed
- WebXRController Button Up/Down state.
- Protect compilation on other platforms.

## [0.5.0] - 2020-12-19
### Added
- Docs page.
- XRInputSource profiles support.
- Added OnControllerActive, OnHandActive and OnHandUpdate actions to WebXRController.

### Changed
- WebXRController no longer instantiate hand joints.
- Better handling for controllers buttons in the editor.
- WebXRController TryUpdateButtons is now private.

### Fixed
- Disable controllers on XR session end.

## [0.4.1] - 2020-11-28
### Added
- Support for Unity 2020.1.
- WebGLTemplates for Unity 2020.1.
- Debug project for Unity 2020.1.

### Changed
- Fixes for JavaScript and Unity communication to support both Unity 2019.1 and Unity 2020.1.
- Added info about OpenUPM package in README.md.
- Invert Y axis for touchpad and thumbstick, so they'll work the same as other XR Providers in Unity.

## [0.4.0] - 2020-11-08
### Added
- Added missing OnXRCapabilitiesUpdate to WebXRManager.
- Added missing controller inputs.

### Changed
- Changed controller action names to enums.

## [0.3.1] - 2020-11-07
### Added
- emscripten browser object pause/resume when switching sessions.

### Changed
- Replaced Inline XR Session with window.rAF.

## [0.3.0] - 2020-11-01
### Added
- Support for XR providers in the editor.
- Settings asset required and optional features in WebXR Session.

### Changed
- Fixed compile and runtime errors, to make sure that WebGL build works again.

## [0.2.0] - 2020-10-30
### Added
- Some XR SDK features, like the subsystems. Still not fully support the XR SDK.
- Info about WebXR Interactions in the README file.
