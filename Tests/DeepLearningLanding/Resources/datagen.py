import os, random, sys
import numpy as np
import cv2
from dutil import *

NUM_IMAGES = %%IMG_COUNT%%
SAMPLES_PER_IMG = 10
DOTS_PER_IMG = 60
IMAGE_W = 144
IMAGE_H = 192
IMAGE_DIR = '../Builds/StreetviewRipper/Output/Images/'
NUM_SAMPLES = NUM_IMAGES * 2 * SAMPLES_PER_IMG
NUM_CHANNELS = 3

print("Compiling...")
x_data = np.empty((NUM_SAMPLES, NUM_CHANNELS, IMAGE_H, IMAGE_W), dtype=np.uint8)
y_data = np.empty((NUM_SAMPLES, 3, IMAGE_H, IMAGE_W), dtype=np.uint8)
ix = 0
im = 0
for root, subdirs, files in os.walk(IMAGE_DIR):
    for file in files:
        path = root + "\\" + file
        if not path[len(path)-12:len(path)] == ".SKY_LDR.jpg": #ignoring masks for now
            continue
        img = cv2.imread(path)
        if img is None:
            assert(False)
        if len(img.shape) != 3 or img.shape[2] != 3:
            assert(False)
        img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        img = cv2.resize(img, (IMAGE_W, IMAGE_H), interpolation = cv2.INTER_LINEAR)
        for i in range(SAMPLES_PER_IMG):
            y_data[ix] = np.transpose(img, (2, 0, 1))
            x_data[ix] = auto_canny(img, (float(i) / SAMPLES_PER_IMG))
            ix += 1
            y_data[ix] = np.flip(y_data[ix - 1], axis=2)
            x_data[ix] = np.flip(x_data[ix - 1], axis=2)
            ix += 1

        sys.stdout.write('\r')
        progress = ix * 100 / NUM_SAMPLES
        sys.stdout.write(str(progress) + "%")
        sys.stdout.flush()
        im += 1
        if im == NUM_IMAGES:
            break

print("\nSaving...")
np.save('x_data.npy', x_data)
np.save('y_data.npy', y_data)