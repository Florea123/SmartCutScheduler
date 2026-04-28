# Build stage

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SmartCutScheduler.Web.csproj .
RUN dotnet restore "SmartCutScheduler.Web.csproj"
# Copy only source files explicitly to avoid exposing sensitive files
COPY _Imports.razor App.razor Program.cs ./
COPY Layout/ Layout/
COPY Models/ Models/
COPY Pages/ Pages/
COPY Services/ Services/
COPY Shared/ Shared/
COPY State/ State/
COPY wwwroot/ wwwroot/
COPY Properties/ Properties/
RUN dotnet publish -c Release -o /app/publish

# Runtime stage (static files)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
