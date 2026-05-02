FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY RazorBlogGenerator.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS generate
WORKDIR /app
COPY --from=build /app .

RUN dotnet RazorBlogGenerator.dll validate
RUN dotnet RazorBlogGenerator.dll build -o /site

FROM nginx:alpine
COPY --from=generate /site /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
CMD ["nginx", "-g", "daemon off;"]
