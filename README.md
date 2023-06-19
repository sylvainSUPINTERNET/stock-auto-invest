# To start

Rename :

__appsettings.Development-TEMPLATE.json__

into 

**appsettings.Development.json**



```` bash


# Install Alpaca 7.0 SDK c#
dotnet restore 


dotnet build


# If EF / db usage
dotnet ef database update 


dotnet watch # or dotnet run, watch seems buggy in 7.0


# Run for production 

dotnet run --environment Production


````