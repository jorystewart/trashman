{
  description = "A cross-platform CLI tool for managing Trash on Linux and Recycle Bin on Windows";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { nixpkgs, self }:
  {
    defaultPackage.x86_64-linux = with nixpkgs.legacyPackages.x86_64-linux; stdenv.mkDerivation {
      name = "trashman";
      buildInputs = [ dotnet-sdk_8 ];

      buildPhase = ''
        dotnet restore
        dotnet build --configuration Release --no-restore trashman.csproj
      '';

      installPhase = ''
        mkdir -p $out/bin
        dotnet publish --configuration Release --no-build --output $out/bin
      '';
    };
  };
}
