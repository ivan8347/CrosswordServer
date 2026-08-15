# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только файлы зависимостей для кэширования слоя
COPY ["CrosswordServer.csproj", "./"]
RUN dotnet restore "CrosswordServer.csproj"

# Копируем весь исходный код и публикуем приложение
COPY . .
RUN dotnet publish "CrosswordServer.csproj" -c Release -o /app/publish

# Этап рантайма
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копируем опубликованные файлы
COPY --from=build /app/publish .

# ВАЖНО: НЕ задаём ASPNETCORE_URLS здесь. Render сам установит порт.
# Если очень нужно явно — используй фиксированный порт 8080, который Render пробросит:
# ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CrosswordServer.dll"]
