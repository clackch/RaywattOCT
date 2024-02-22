from ultralytics import YOLO

# Load a model
model = YOLO('.\\export\\detect.pt')

# Export the model
model.export(format='engine', device=0)