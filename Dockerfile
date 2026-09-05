FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY . .
RUN dotnet restore RazorBlogGenerator/RazorBlogGenerator.csproj
RUN dotnet publish RazorBlogGenerator/RazorBlogGenerator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS generate
WORKDIR /app
COPY --from=build /app .
RUN apk add --no-cache font-dejavu

RUN dotnet RazorBlogGenerator.dll validate
RUN dotnet RazorBlogGenerator.dll build -o /site

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine
WORKDIR /app
COPY --from=generate /app .
COPY --from=generate /site /app/dist
ENV PORT=80
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
	CMD wget --no-verbose --tries=1 --spider "http://127.0.0.1:${PORT}/health" || exit 1
ENTRYPOINT ["dotnet", "RazorBlogGenerator.dll", "serve"]
