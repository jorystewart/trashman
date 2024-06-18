function Delete-Trash
{
  param(
    [Parameter(Mandatory=$true,Position=0)][ValidateNotNullOrEmpty][System.Collections.Generic.List[string]]$File
  )
}