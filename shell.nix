{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  name = "moth-lang-env";
  packages = with pkgs; [ dotnetCorePackages.sdk_8_0 ];
  buildInputs = with pkgs; [
    msbuild
    clang_16
    git
    git-extras
  ];
}
