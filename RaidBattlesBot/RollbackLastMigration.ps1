$migrations = dotnet ef migrations list --prefix-output --json |
        where { $_.StartsWith('data:') } |
        foreach { $_.Substring(5) } |
        ConvertFrom-Json

dotnet ef database update $migrations[-2].SafeName --no-build
dotnet ef migrations remove --no-build