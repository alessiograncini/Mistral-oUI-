# Mistral-oUI-
A contextual interface visualizer powered by Mistral and MoonDream Vision


# How to run the server 

1. get bun `curl -fsSL https://bun.sh/install | bash`
2. get poetry `curl -sSL https://install.python-poetry.org | python3 -`
3. cd `server` then `bun install` and then `poetry install`
4. Run `bun start` this will run both the servers.
5. Wait for python server to start, might take a min especially on first run. will look like below
![<CleanShot 2024-03-24 at 06.22.34@2x.png>](https://github.com/CryogenicPlanet/mistral-hack/assets/10355479/a74ac860-997a-48fa-84b2-47fe75a07d9d)

6. To test you can use this `curl` script

```bash
curl -X POST "http://localhost:8000/upload-image/" -F "file=@path/to/your/dummy/file.jpg"
```

# How to run unity 

Please use Unity 2023.2.2f1
Build the scene named "Mistral" 
Or install the .APK in the build folder 