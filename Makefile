# Name of your projects
PROJECT1=RestroPlate.IdentityService
PROJECT2=RestroPlate.DonationService
PROJECT3=RestroPlate.InventoryService
PROJECT4=RestroPlate.PublicService

# Default target
all: run-all

# Run all projects
run-all:
	dotnet run --project $(PROJECT1) &
	dotnet run --project $(PROJECT2) &
	dotnet run --project $(PROJECT3) &
	dotnet run --project $(PROJECT4) &
	wait

# Run projects individually
run-project1:
	dotnet run --project $(PROJECT1)

run-project2:
	dotnet run --project $(PROJECT2)

run-project3:
	dotnet run --project $(PROJECT3)
	
run-project4:
	dotnet run --project $(PROJECT4)

# Clean build outputs
clean:
	dotnet clean $(PROJECT1)
	dotnet clean $(PROJECT2)
	dotnet clean $(PROJECT3)