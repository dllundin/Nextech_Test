# AI Angular Client

Quick start:

1. Start the API (from repository root):

```powershell
cd Api
dotnet run
```

2. Start the Angular client:

```powershell
cd ClientApp
npm install
npx ng serve --proxy-config proxy.conf.json
```

The client proxies `/WeatherForecast` to `http://localhost:5000/WeatherForecast`.
