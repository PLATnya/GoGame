# GoGame

Board game "Go", created for studying purposes as a project for the course of learning the C # language
Have 2 local players and board of size 9X9

![](https://drive.google.com/uc?export=view&id=1_9us3PojLCDSC5xf4njrwD8XLVAgmLgw)
_____
## Implemented rules
- Opponent stones that you have surrounded with your stones are removed from the board and added to your score
- You can't go to "suicidal" points

![](https://drive.google.com/uc?export=view&id=1QIsjF5CftujYq6n1d9IjFOBj8GpVX97v)

### Not imlemented
- "Ko" rule
_____
## Winning
The winner is determined by the points scored, after a pass from both sides

![](https://drive.google.com/uc?export=view&id=15IRbOyNVgegh32zkTh2r3I2CjS_SHe5V)

_____
## Deploying

If you want(why?) to play the game you will need to use `dotnet publish` command

[.NET Deploying Microsoft info](https://docs.microsoft.com/dotnet/core/deploying/)

Copy the repository
```
git clone https://github.com/PLATnya/GoGame.git
cd GoGame
```
Make some magic

`dotnet publish -c Release -r <RID> --self-contained true`

where <RID> - Runtime identifier for custom platform

[choose your RID](https://docs.microsoft.com/dotnet/core/rid-catalog)

### Example

```
dotnet publish -c Release -r win-x64 --self-contained true
cd Game/bin/Release/netcoreapp3.1/win-x64/publish
./Game.exe
```

then enjoy it!

