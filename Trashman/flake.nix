{
  description = "A cross-platform CLI tool for managing Trash on Linux and Recycle Bin on Windows";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { nixpkgs, self }:
  {
    system = "x86_64-linux";
    pkgs = import nixpkgs { inherit system; };
    dotnetProgram = pkgs.buildDotnetModule rec {
      pname = "trashman";
      version = "0.1";
      buildInputs = [ dotnet-sdk_8 ];

      src = pkgs.fetchFromGitHub {
        owner = "jorystewart";
        repo = pname;
        rev = "v${version}";
        sha256 = "";
      };

      projectFile = "Trashman/trashman.csproj";

      nugetDeps = # TODO

      meta = with pkgs.lib; {
        homepage = "some_homepage";
        description = "some_description";
        license = licenses.mit;
      };
    };
  in
  {
    packages.${system}.dotnetProgram = dotnetProgram;
  };
}
