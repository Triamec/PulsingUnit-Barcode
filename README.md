# Fire a Barcode with the Pulsing Unit

 [![TAM - Tama](https://img.shields.io/static/v1?label=TAM&message=Tama&color=b51839)](https://www.triamec.com/tam-tama.html)

This example shows how a barcode like pattern, configured in Fifo mode.

- The program doesn't depend on a specific axis unit, nevertheless it is designed for the example in mm.
- The start position of the barcode is at 20mm.
- The peripheral system is triggered with a pulse ON-time of 10us.
- The gaps are pulsed with an ON-time of 0 seconds, which results in no pulse output.

The whole pattern consists of 2 rows with 5 sequences each. The second row is fired in reverse direction.

![Pattern](doc/BarcodeExample.png)

## Program Description

1. Set the `PU_ReferencePosition = 20`, defining the start position of the first row.
2. Activate the Pulsing Unit by setting the `PU_Mode = Fifo`.
3. Fill in the first row by pushing each sequence to the FIFO with the command `PU_FIFO = Append`.

| Sequence         | 1       | 2   | 3       | 4   | 5       |
| ---------------- | ------- | --- | ------- | --- | ------- |
| PU_PulseWidth    | 0.00001 | 0   | 0.00001 | 0   | 0.00001 |
| PU_DeltaPosition | 0.1     | 0.1 | 0.1     | 0.1 | 0.1     |
| PU_Count         | 8       | 13  | 21      | 26  | 31      |

4. Now the row is fired by moving the axis left to right (positive direction).
5. As soon as the first row is fired (check for `PU_Count == PU_ActualPulseCount`), a move with an other axis can be commanded to align to the next row.
6. Set a new reference position with the following steps.
   1. Disable the mode by setting `PU_Mode = Disabled`.
   2. Set the new start position for row 2 with `PU_ReferencePosition = 20.3`.
   3. In the next cycle, activate the Pulsing Unit by setting the `PU_Mode = Fifo`.
7. Fill in the second row by pushing each sequence to the FIFO with the command `PU_FIFO = Append`. The sequence index restarts at 1 because the mode was reset.

| Sequence         | 1       | 2   | 3       | 4   | 5       |
| ---------------- | ------- | --- | ------- | --- | ------- |
| PU_PulseWidth    | 0.00001 | 0   | 0.00001 | 0   | 0.00001 |
| PU_DeltaPosition | -0.1    | -0.1| -0.1    | -0.1| -0.1    |
| PU_Count         | 5       | 10  | 18      | 23  | 31      |

8. Now the second row is fired by moving the axis right to left (negative direction).
