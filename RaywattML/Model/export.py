from ultralytics import YOLO

# Load a model
model = YOLO('.\\export\\segment.pt')

# Export the model
model.export(format='engine', device=0)