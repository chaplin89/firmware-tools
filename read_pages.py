#! /usr/bin/env python2
import serial
import math

# Size of the nand flash in bytes
SIZE=128*1024*1024
# Size of a single page
PAGESIZE=2048
# Number of pages
PAGENUMBER=int(math.floor(SIZE/PAGESIZE))

# Lenght of the meaningful bytes returned by the command "nand read".
PAGE_LENGHT_BYTES=6430
# Output file path
OUTPUT_FILE='dump.bin'
# Serial device
SERIAL='/dev/ttyUSB0'
# Baudrate
BAUDRATE=115200

ser = serial.Serial(SERIAL, BAUDRATE, timeout=1)
ser.reset_input_buffer()

index=0
with open(OUTPUT_FILE, 'w') as file:
    while (index<PAGENUMBER):
        print('Dump page {}/{}'.format(hex(index), hex(PAGENUMBER)))
        ser.write(b'nand page {}\n'.format(hex(index)))
        # First line should contain the page number.
        file.write(ser.readline())
        # Other lines contains data.
        file.write(ser.read(PAGE_LENGHT_BYTES))
        # Not interested about anything that come later.
        ser.reset_input_buffer()
        index=index+1