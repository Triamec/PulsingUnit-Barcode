using System;
using System.Linq;
using Triamec.Tama.Rlid19;
using Triamec.Tama.Vmid5;
using Triamec.TriaLink;

// The class name (as well as the project name and the namespace) contributes to the file name of the produced Tama
// program. This file is located in the bin\Debug or bin\Release subfolders and will commonly be copied into the
// Tama directory of the default workspace, too.
[Tama]
internal static class BarcodePulses
{

    // Linear State Machine, shown in Register.Application.TamaControl.IsochronousMainState
    private enum State
    {
        Idle,
        MoveToStart,
        FillFifoPositive,
        MoveRowPositive,
        FillFifoNegative,
        MoveRowNegative
    }

    // User commands, invoked at Register.Application.TamaControl.IsochronousMainCommand
    private static class Command
    {
        public const int None = 0;
        public const int Go = 1;
    }

    private static void ResetPU()
    {
        Register.Axes_0.Commands.OptionModule.PwmOut = 0;
        Register.Axes_0.Commands.OptionModule.PU_Output = OptionPuOutput.Disabled;
        Register.Axes_0.Commands.OptionModule.PU_Source = OptionPuSource.EncoderFast;
        Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Disabled;
        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.None;
        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = 0;
        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = 0;
        Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = 0;
        Register.Axes_0.Commands.OptionModule.PU_Count = 0;
        Register.Axes_0.Commands.OptionModule.PU_DelayTime = 0;
    }

    private static void PreparePUPositive()
    {
        Register.Axes_0.Commands.OptionModule.PwmOut = 1;
        Register.Axes_0.Commands.OptionModule.PU_Output = OptionPuOutput.TTL;
        Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Fifo;
        Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferencePositive;

    }

    private static void PreparePUNegative()
    {
        Register.Axes_0.Commands.OptionModule.PwmOut = 1;
        Register.Axes_0.Commands.OptionModule.PU_Output = OptionPuOutput.TTL;
        Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Fifo;
        Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferenceNegative;
    }

    // pulsing application constants
    const int cRows = 2;
    const float cMoveStartPosition = 19.9f;
    const float cReferencePositive = 20f;
    const float cDeltaPosition = 0.1f;
    const float cPulseWidth = 0.00001f;
    const float cMoveEndPosition = 31 * cDeltaPosition + cMoveStartPosition;
    const float cReferenceNegative = cMoveEndPosition - cDeltaPosition;

    // internal variables
    const int numberOfSegments = 5;
    static int _row_index;
    static readonly int[] cPulseCountPositive = new int[numberOfSegments];
    static readonly int[] cPulseCountNegative = new int[numberOfSegments];
    static readonly int[] cPulseOn = new int[numberOfSegments];
    static int segmentIndex;

    // Constructor
    static BarcodePulses()
    {
        cPulseCountPositive[0] = 8;
        cPulseCountPositive[1] = 5;
        cPulseCountPositive[2] = 8;
        cPulseCountPositive[3] = 5;
        cPulseCountPositive[4] = 5;

        cPulseCountNegative[0] = 5;
        cPulseCountNegative[1] = 5;
        cPulseCountNegative[2] = 8;
        cPulseCountNegative[3] = 5;
        cPulseCountNegative[4] = 8;

        cPulseOn[0] = 1;
        cPulseOn[1] = 0;
        cPulseOn[2] = 1;
        cPulseOn[3] = 0;
        cPulseOn[4] = 1;

        ResetPU();

        // setting command to idle to avoid automatic start
        Register.Application.TamaControl.IsochronousMainCommand = 0;

    }
    // -- entry point --
    [TamaTask(Task.IsochronousMain)]
    static void Main()
    {
        switch ((State)Register.Application.TamaControl.IsochronousMainState)
        {
            case State.Idle:
                switch (Register.Application.TamaControl.IsochronousMainCommand)
                {
                    case Command.Go:
                        _row_index = 0;
                        segmentIndex = 0;
                        MoveTo(cMoveStartPosition);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveToStart;
                        ResetPU() ;
                        break;
                }

                break;
            case State.MoveToStart:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoPositive;
                    PreparePUPositive();
                }
                break;
            case State.FillFifoPositive:
                if (_row_index < cRows)
                {
                    if (segmentIndex < numberOfSegments)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = cDeltaPosition;
                        Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferencePositive;
                        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = cPulseWidth * cPulseOn[segmentIndex];
                        Register.Axes_0.Commands.OptionModule.PU_Count = Register.Axes_0.Commands.OptionModule.PU_Count +
                            (uint)cPulseCountPositive[segmentIndex];
                        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.Append;
                        segmentIndex++;
                    }
                    else
                    {
                        MoveTo(cMoveEndPosition);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveRowPositive;
                    }
                }
                else
                {
                    Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                }
                break;
            case State.MoveRowPositive:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Disabled;
                    Register.Axes_0.Commands.OptionModule.PU_Count = 0;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoNegative;
                    PreparePUNegative();
                    segmentIndex = 0;
                    _row_index++;

                    // -- Move other Axis to align with next row --
                }
                break;
            case State.FillFifoNegative:
                if (_row_index < cRows)
                {
                    if (segmentIndex < numberOfSegments)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = -cDeltaPosition;
                        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = cPulseWidth * cPulseOn[segmentIndex];
                        Register.Axes_0.Commands.OptionModule.PU_Count += (uint)cPulseCountNegative[segmentIndex];
                        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.Append;
                        segmentIndex++;
                    }
                    else
                    {
                        MoveTo(cMoveStartPosition);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveRowNegative;
                    }
                }
                else
                {
                    Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                }
                break;
            case State.MoveRowNegative:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    _row_index++;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;

                    // -- Move other Axis to align with next row --
                }
                break;
        }
    }

    static void MoveTo(float position)
    {
        Register.Axes_0.Commands.PathPlanner.Xnew = position;
        Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
    }
}