using System;
using System.Collections.Generic;
using Engine;

namespace Game
{
    public class CommandElectricElement : ElectricElement
    {
        public SubsystemCommand m_subsystemCommand;

        public CommandData m_commandData;

        public float m_voltage;

        public bool clockAllowed = true;

        public SubmitResult m_submitResult;

        public CommandElectricElement(SubsystemElectricity subsystemElectricity, Point3 position)
            : base(subsystemElectricity, new List<CellFace>
            {
                new CellFace(position.X, position.Y, position.Z, 0),
                new CellFace(position.X, position.Y, position.Z, 1),
                new CellFace(position.X, position.Y, position.Z, 2),
                new CellFace(position.X, position.Y, position.Z, 3),
                new CellFace(position.X, position.Y, position.Z, 4),
                new CellFace(position.X, position.Y, position.Z, 5)
            })
        {
            m_commandData = subsystemElectricity.Project.FindSubsystem<SubsystemCommandBlockBehavior>(throwOnError: true).GetCommandData(position);
            m_subsystemCommand = subsystemElectricity.Project.FindSubsystem<SubsystemCommand>(throwOnError: true);
            m_submitResult = SubmitResult.Fail;
        }

        public override float GetOutputVoltage(int face)
        {
            return m_voltage;
        }

        public override bool Simulate()
        {
            try
            {
                if (m_commandData == null)
                {
                    return false;
                }

                if (m_commandData.Mode == WorkingMode.Default)
                {
                    if (CalculateHighInputsCount() > 0)
                    {
                        m_submitResult = m_subsystemCommand.Submit(m_commandData.Name, m_commandData, Judge: false);
                        m_subsystemCommand.ShowSubmitTips(string.Empty, onlyError: true, m_submitResult, m_commandData);
                        return m_submitResult == SubmitResult.Success;
                    }
                }
                else
                {
                    if (m_commandData.Mode == WorkingMode.Condition)
                    {
                        base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + 1);
                        if (m_submitResult != 0 && m_submitResult != SubmitResult.Fail)
                        {
                            return false;
                        }

                        m_submitResult = m_subsystemCommand.Submit(m_commandData.Name, m_commandData, Judge: true);
                        if (m_submitResult != SubmitResult.OutRange && m_submitResult != SubmitResult.Invalid)
                        {
                            m_subsystemCommand.ShowSubmitTips(string.Empty, onlyError: true, m_submitResult, m_commandData);
                        }

                        m_voltage = ((m_submitResult == SubmitResult.Success) ? 1f : 0f);
                        return true;
                    }

                    if (m_commandData.Mode == WorkingMode.Variable)
                    {
                        m_voltage = 0f;
                        int[] signals = GetSignals();
                        int num = signals[4];
                        if (IsVariableSyncMode())
                        {
                            if (num >= 8 && clockAllowed)
                            {
                                clockAllowed = false;
                                return VariableSubmit(signals);
                            }

                            if (num < 8)
                            {
                                clockAllowed = true;
                            }
                        }
                        else if (signals[0] > 0 || signals[1] > 0 || signals[2] > 0 || signals[3] > 0)
                        {
                            return VariableSubmit(signals);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("CommandElectricElement:" + ex.Message);
            }

            return false;
        }

        public bool VariableSubmit(int[] signals)
        {
            m_commandData = DataHandle.SetVariableData(m_commandData, signals);
            m_submitResult = m_subsystemCommand.Submit(m_commandData.Name, m_commandData, Judge: false);
            m_subsystemCommand.ShowSubmitTips(string.Empty, onlyError: true, m_submitResult, m_commandData);
            if (m_submitResult == SubmitResult.Success)
            {
                m_voltage = 1f;
                return true;
            }

            m_voltage = 0f;
            return false;
        }

        public int[] GetSignals()
        {
            int[] array = new int[6];
            foreach (ElectricConnection connection in base.Connections)
            {
                if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
                {
                    switch (connection.NeighborConnectorFace)
                    {
                        case 0:
                            array[0] = (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(0) * 15f);
                            break;
                        case 1:
                            array[1] = (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(1) * 15f);
                            break;
                        case 2:
                            array[2] = (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(2) * 15f);
                            break;
                        case 3:
                            array[3] = (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(3) * 15f);
                            break;
                        case 4:
                            array[4] = (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(4) * 15f);
                            break;
                        case 5:
                            array[5] = (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(5) * 15f);
                            break;
                    }
                }
            }

            return array;
        }

        public bool IsVariableSyncMode()
        {
            bool result = false;
            foreach (ElectricConnection connection in base.Connections)
            {
                if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
                {
                    ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(base.CellFaces[0].Face, 0, connection.ConnectorFace);
                    if (connectorDirection.HasValue && connectorDirection == ElectricConnectorDirection.Bottom)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }
    }
}