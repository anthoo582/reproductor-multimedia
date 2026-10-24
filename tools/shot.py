import sys

from PIL import ImageGrab

left, top, right, bottom = map(int, sys.argv[1:5])
ImageGrab.grab(bbox=(left, top, right, bottom), all_screens=True).save(sys.argv[5])
