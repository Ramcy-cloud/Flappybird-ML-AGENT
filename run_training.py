import sys
import os
import multiprocessing

if __name__ == '__main__':
    multiprocessing.freeze_support()
    os.environ["PYTHONUNBUFFERED"] = "1"
    from mlagents.trainers.learn import main
    main()
