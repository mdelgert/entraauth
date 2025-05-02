dotnet dev-certs https --clean
dotnet dev-certs https --trust
docker-compose down
docker-compose up --build