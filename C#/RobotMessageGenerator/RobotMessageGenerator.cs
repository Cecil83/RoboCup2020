﻿using System;
using EventArgsLibrary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Constants;

namespace RobotMessageGenerator
{
    public class RobotMsgGenerator
    {
        //Input events
        public void GenerateMessageSetSpeedConsigneToRobot(object sender, SpeedConsigneArgs e)
        {
            byte[] payload = new byte[12];
            //Int32 Vx = (Int32)(e.Vx*1000);
            //Int32 Vy = (Int32)(e.Vy * 1000);
            //Int32 Vtheta = (Int32)(e.Vtheta * 1000);

            //payload.SetValueRange(Vx.GetBytes(), 0);
            //payload.SetValueRange(Vy.GetBytes(), 4);
            //payload.SetValueRange(Vtheta.GetBytes(), 8);


            payload.SetValueRange((e.Vx).GetBytes(), 0);
            payload.SetValueRange((e.Vy).GetBytes(), 4);
            payload.SetValueRange((e.Vtheta).GetBytes(), 8);

            OnMessageToRobot((Int16)Commands.SetSpeedConsigne, 12, payload);
        }

        public void GenerateMessageSetSpeedConsigneToMotor(object sender, SpeedConsigneToMotorArgs e)
        {
            byte[] payload = new byte[5];
            payload.SetValueRange(e.V.GetBytes(), 0);
            payload[4] = (byte)e.MotorNumber;
            OnMessageToRobot((Int16)Commands.SetMotorSpeedConsigne, 5, payload);
        }
        public void GenerateMessageTir(object sender, TirEventArgs e)
        {
            byte[] payload = new byte[4];
            payload.SetValueRange(e.Puissance.GetBytes(), 0);
            OnMessageToRobot((Int16)Commands.TirCommand, 4, payload);
        }
        public void GenerateMessageMoveTirUp(object sender, EventArgs e)
        {
            OnMessageToRobot((Int16)Commands.MoveTirUp, 0, null);
        }
        
        public void GenerateMessageMoveTirDown(object sender, EventArgs e)
        {
            OnMessageToRobot((Int16)Commands.MoveTirDown, 0, null);
        }

        public void GenerateMessageEnableDisableMotors(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] =Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableDisableMotors, 1, payload);
        }

        public void GenerateMessageEnableDisableTir(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableDisableTir, 1, payload);
        }

        public void GenerateMessageEnableAsservissement(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableAsservissement, 1, payload);
        }

        public void GenerateMessageEnablePIDDebugData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnablePIDDebugData, 1, payload);
        }

        public void GenerateMessageEnableEncoderRawData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableEncoderRawData, 1, payload);
        }

        public void GenerateMessageEnableMotorCurrentData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableMotorCurrent, 1, payload);
        }

        public void GenerateMessageEnableMotorPositionData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnablePositionData, 1, payload);
        }

        public void GenerateMessageEnableMotorSpeedConsigne(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableMotorSpeedConsigne, 1, payload);
        }

        public void GenerateMessageSTOP(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);

            OnMessageToRobot((Int16)Commands.EmergencySTOP, 1, payload);
        }

        public void GenerateMessageSetPIDValueToRobot(object sender, PIDDataArgs e)
        {
            byte[] payload = new byte[36];
            payload.SetValueRange(((float)(e.P_x)).GetBytes(), 0);
            payload.SetValueRange(((float)(e.I_x)).GetBytes(), 4);
            payload.SetValueRange(((float)(e.D_x)).GetBytes(), 8);
            payload.SetValueRange(((float)(e.P_y)).GetBytes(), 12);
            payload.SetValueRange(((float)(e.I_y)).GetBytes(), 16);
            payload.SetValueRange(((float)(e.D_y)).GetBytes(), 20);
            payload.SetValueRange(((float)(e.P_theta)).GetBytes(), 24);
            payload.SetValueRange(((float)(e.I_theta)).GetBytes(), 28);
            payload.SetValueRange(((float)(e.D_theta)).GetBytes(), 32);
            OnMessageToRobot((Int16)Commands.SetPIDValues, 36, payload);
        }
        //public void GenerateTextMessage(object sender, EventArgsLibrary.SpeedConsigneArgs e)
        //{
        //    byte[] payload = new byte[12];
        //    payload.SetValueRange(e.Vx.GetBytes(), 0);
        //    payload.SetValueRange(e.Vy.GetBytes(), 4);
        //    payload.SetValueRange(e.Vtheta.GetBytes(), 8);
        //    OnMessageToRobot(Commands., 12, payload);
        //}

        //Output events
        public delegate void SpeedConsigneEventHandler(object sender, MessageToRobotArgs e);
        public event EventHandler<MessageToRobotArgs> OnMessageToRobotGeneratedEvent;
        public virtual void OnMessageToRobot(Int16 msgFunction, Int16 msgPayloadLength, byte[] msgPayload)
        {
            var handler = OnMessageToRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new MessageToRobotArgs { MsgFunction = msgFunction, MsgPayloadLength = msgPayloadLength, MsgPayload=msgPayload});
            }
        }
    }
}
