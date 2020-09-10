FROM mcr.microsoft.com/dotnet/core/runtime:3.1

COPY /RoleHandelerBot/bin/Release/netcoreapp3.1/publish/ app/

ENTRYPOINT dotnet app/RoleHandelerBot.dll "$MONGO_URL" "$ENVIRONMENT" "$TOKEN" "$PREFIX"