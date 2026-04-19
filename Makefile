# Name of your projects
PROJECT1=RestroPlate.IdentityService
PROJECT2=RestroPlate.DonationService
PROJECT3=RestroPlate.InventoryService
PROJECT4=RestroPlate.PublicService

# Container registry + image tag (override at runtime if needed)
REGISTRY?=restroplate
TAG?=latest

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

# Build and push all microservice images using repository root as Docker context.
# Example: make build-push-images REGISTRY=ghcr.io/my-org TAG=v1.0.0
build-push-images:
	docker build -f RestroPlate.IdentityService/Dockerfile -t $(REGISTRY)/identity-service:$(TAG) .
	docker push $(REGISTRY)/identity-service:$(TAG)
	docker build -f RestroPlate.DonationService/Dockerfile -t $(REGISTRY)/donation-service:$(TAG) .
	docker push $(REGISTRY)/donation-service:$(TAG)
	docker build -f RestroPlate.InventoryService/Dockerfile -t $(REGISTRY)/inventory-service:$(TAG) .
	docker push $(REGISTRY)/inventory-service:$(TAG)
	docker build -f RestroPlate.PublicService/Dockerfile -t $(REGISTRY)/public-service:$(TAG) .
	docker push $(REGISTRY)/public-service:$(TAG)