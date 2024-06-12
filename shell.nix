let
  nixpkgsVer = "24.05";
  pkgs = import (fetchTarball "https://github.com/NixOS/nixpkgs/tarball/nixos-${nixpkgsVer}") { config = {}; overlays = []; };
  sdk = pkgs.dotnetCorePackages.sdk_8_0;
  clang = pkgs.clang_16;
  priorityDeps = [ sdk clang ];
  DOTNET_ROOT = "${sdk.out}";
  DOTNET_HOST_PATH = "${DOTNET_ROOT}/bin/dotnet";
in pkgs.mkShell {
  name = "moth-lang";

  inherit DOTNET_ROOT;
  inherit DOTNET_HOST_PATH;

  buildInputs = with pkgs; priorityDeps ++ [
    git
    git-extras
  ];

  __SILK_INCLUDE_GLIBC = "${pkgs.glibc.dev}/include";
  
  # can't debug without this
  LIBCLANG_DISABLE_CRASH_RECOVERY = 1;
}
