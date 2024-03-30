from ultralytics import YOLO
from ultralytics.utils.plotting import Annotator, colors
import cv2
import numpy as np


def predict_object(img):
    """
        Args:
        img: Numpy tensor (H, W, 3)

        Return:
        objects (list) : [[name, xywh]*]
    """

    print("predicting object")
    model = YOLO("./yolov8n.pt")  # Load model
    results = model.predict(source=img)  # predict

    if len(results) < 1:
        print("WARNING! No object detected by YOLO.")
        return []

    boxes = results[0].boxes.xywh
    names = results[0].names
    detected_classes = [int(i.item()) for i in results[0].boxes.cls]

    uniq_classes, counts = np.unique(detected_classes, return_counts=True)
    uniq_dict = {u: c for u, c in zip(uniq_classes, counts)}

    objects = []
    for i in range(len(detected_classes)):
        objects.append([names[detected_classes[i]] + '_' +
                       str(uniq_dict[detected_classes[i]]), boxes[i].tolist()])
        uniq_dict[detected_classes[i]] -= 1

    print("Done predicting object")

    ann = Annotator(
        img,
        line_width=None,  # default auto-size
        font_size=None,   # default auto-size
        font="Arial.ttf",  # must be ImageFont compatible
        pil=False,        # use PIL, otherwise uses OpenCV
    )

    for i, (name, bbox) in enumerate(objects):
        ann.box_label(results[0].boxes.xyxy[i], name,
                      color=colors(i, bgr=True))

    annotated_image = ann.result()

    return objects, annotated_image


if __name__ == "__main__":

    # from ndarray
    image = cv2.imread("/Users/rohanc/Downloads/croisant.jpg")
