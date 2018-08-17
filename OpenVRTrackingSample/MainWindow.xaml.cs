using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Valve.VR;
using System.Windows.Threading;

namespace OpenVRTrackingSample
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        static CVRSystem system;
        private Thread VRThread;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            VRThread = new Thread(VRInit);
            VRThread.Start();
        }

        private String GetDeviceState(ETrackingResult eTrackingResult)
        {
            switch (eTrackingResult)
            {
                case ETrackingResult.Uninitialized:
                    return nameof(ETrackingResult.Uninitialized);
                case ETrackingResult.Calibrating_InProgress:
                    return nameof(ETrackingResult.Calibrating_InProgress);
                case ETrackingResult.Calibrating_OutOfRange:
                    return nameof(ETrackingResult.Calibrating_OutOfRange);
                case ETrackingResult.Running_OK:
                    return nameof(ETrackingResult.Running_OK);
                case ETrackingResult.Running_OutOfRange:
                    return nameof(ETrackingResult.Running_OutOfRange);
                default:
                    return "";
            }
        }

        // Get the quaternion representing the rotation
        private HmdQuaternion_t GetRotation(HmdMatrix34_t matrix)
        {
            HmdQuaternion_t q;

            q.w = Math.Sqrt(Math.Max(0, 1 + matrix.m0 + matrix.m5 + matrix.m10)) / 2;
            q.x = Math.Sqrt(Math.Max(0, 1 + matrix.m0 - matrix.m5 - matrix.m10)) / 2;
            q.y = Math.Sqrt(Math.Max(0, 1 - matrix.m0 + matrix.m5 - matrix.m10)) / 2;
            q.z = Math.Sqrt(Math.Max(0, 1 - matrix.m0 - matrix.m5 + matrix.m10)) / 2;
            q.x = Copysign(q.x, matrix.m9 - matrix.m6);
            q.y = Copysign(q.y, matrix.m2 - matrix.m8);
            q.z = Copysign(q.z, matrix.m4 - matrix.m1);
            return q;
        }

        //CopySign C# Customized Function
        private double Copysign(double x, double y)
        {
            double sign = y / Math.Abs(y);
            double absX = Math.Abs(x);
            double copy = absX * sign;
            return copy;
        }

        // Get the vector representing the position
        private HmdVector3_t GetPosition(HmdMatrix34_t matrix)
        {
            HmdVector3_t vector;

            vector.v0 = matrix.m3;
            vector.v1 = matrix.m7;
            vector.v2 = matrix.m11;
            return vector;
        }

        private void VRInit()
        {
            // init
            var error = EVRInitError.None;
            
            system = OpenVR.Init(ref error);
            if (error != EVRInitError.None) throw new Exception();

            OpenVR.GetGenericInterface(OpenVR.IVRSystem_Version, ref error);
            if (error != EVRInitError.None) throw new Exception();

            TrackedDevicePose_t[] m_rTrackedDevicePose = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            TrackedDevicePose_t TrackedDevicePose = new TrackedDevicePose_t();
            VRControllerState_t controllerState = new VRControllerState_t();
            while (true)
            {
                system.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, m_rTrackedDevicePose);

                for (uint unDevice = 0; unDevice < OpenVR.k_unMaxTrackedDeviceCount; unDevice++)
                {
                    if (!system.IsTrackedDeviceConnected(unDevice))
                    {
                        continue;
                    }

                    HmdVector3_t position;
                    HmdQuaternion_t quaternion;

                    // Get what type of device it is and work with its data
                    ETrackedDeviceClass trackedDeviceClass = system.GetTrackedDeviceClass(unDevice);
                    ETrackedControllerRole trackedControllerRole = system.GetControllerRoleForTrackedDeviceIndex(unDevice);
                    ETrackingResult eTrackingResult;

                    switch (trackedDeviceClass)
                    {
                        case ETrackedDeviceClass.HMD:

                            // get the position and rotation
                            position = GetPosition(m_rTrackedDevicePose[unDevice].mDeviceToAbsoluteTracking);
                            quaternion = GetRotation(m_rTrackedDevicePose[unDevice].mDeviceToAbsoluteTracking);

                            // for printing some more info to the user about the state of the device/pose
                            eTrackingResult = m_rTrackedDevicePose[unDevice].eTrackingResult;
                            
                            // print the tracking data
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate {
                                HmdIndex.Text = String.Format("{0}", unDevice);
                                HmdVectorX.Text = String.Format("{0:F4}", position.v0);
                                HmdVectorY.Text = String.Format("{0:F4}", position.v1);
                                HmdVectorZ.Text = String.Format("{0:F4}", position.v2);
                                HmdQuaternionX.Text = String.Format("{0:F4}", quaternion.x);
                                HmdQuaternionY.Text = String.Format("{0:F4}", quaternion.y);
                                HmdQuaternionZ.Text = String.Format("{0:F4}", quaternion.z);
                                HmdQuaternionW.Text = String.Format("{0:F4}", quaternion.w);
                                HmdState.Text = GetDeviceState(eTrackingResult);
                            }));
                            break;
                        case ETrackedDeviceClass.Controller:

                            // get the position and rotation
                            position = GetPosition(m_rTrackedDevicePose[unDevice].mDeviceToAbsoluteTracking);
                            quaternion = GetRotation(m_rTrackedDevicePose[unDevice].mDeviceToAbsoluteTracking);

                            // for printing some more info to the user about the state of the device/pose
                            eTrackingResult = m_rTrackedDevicePose[unDevice].eTrackingResult;

                            // get Controllers info
                            system.GetControllerStateWithPose(ETrackingUniverseOrigin.TrackingUniverseStanding, unDevice, ref controllerState, (uint)Marshal.SizeOf(controllerState), ref TrackedDevicePose);

                            switch (trackedControllerRole)
                            {
                                case ETrackedControllerRole.LeftHand:

                                    // print the tracking data
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate {
                                        CLIndex.Text = String.Format("{0}", unDevice);
                                        CLVectorX.Text = String.Format("{0:F4}", position.v0);
                                        CLVectorY.Text = String.Format("{0:F4}", position.v1);
                                        CLVectorZ.Text = String.Format("{0:F4}", position.v2);
                                        CLQuaternionX.Text = String.Format("{0:F4}", quaternion.x);
                                        CLQuaternionY.Text = String.Format("{0:F4}", quaternion.y);
                                        CLQuaternionZ.Text = String.Format("{0:F4}", quaternion.z);
                                        CLQuaternionW.Text = String.Format("{0:F4}", quaternion.w);
                                        CLState.Text = GetDeviceState(eTrackingResult);
                                        CLulPressed.Text = String.Format("{0}", controllerState.ulButtonPressed);
                                        CLulTouched.Text = String.Format("{0}", controllerState.ulButtonTouched);
                                        CLAxis0X.Text = String.Format("{0:F4}", controllerState.rAxis0.x);
                                        CLAxis0Y.Text = String.Format("{0:F4}", controllerState.rAxis0.y);
                                        CLAxis1X.Text = String.Format("{0:F4}", controllerState.rAxis1.x);
                                        CLAxis1Y.Text = String.Format("{0:F4}", controllerState.rAxis1.y);
                                        CLAxis2X.Text = String.Format("{0:F4}", controllerState.rAxis2.x);
                                        CLAxis2Y.Text = String.Format("{0:F4}", controllerState.rAxis2.y);
                                        CLAxis3X.Text = String.Format("{0:F4}", controllerState.rAxis3.x);
                                        CLAxis3Y.Text = String.Format("{0:F4}", controllerState.rAxis3.y);
                                        CLAxis4X.Text = String.Format("{0:F4}", controllerState.rAxis4.x);
                                        CLAxis4Y.Text = String.Format("{0:F4}", controllerState.rAxis4.y);
                                    }));
                                    break;

                                case ETrackedControllerRole.RightHand:

                                    // print the tracking data
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate {
                                        CRIndex.Text = String.Format("{0}", unDevice);
                                        CRVectorX.Text = String.Format("{0:F4}", position.v0);
                                        CRVectorY.Text = String.Format("{0:F4}", position.v1);
                                        CRVectorZ.Text = String.Format("{0:F4}", position.v2);
                                        CRQuaternionX.Text = String.Format("{0:F4}", quaternion.x);
                                        CRQuaternionY.Text = String.Format("{0:F4}", quaternion.y);
                                        CRQuaternionZ.Text = String.Format("{0:F4}", quaternion.z);
                                        CRQuaternionW.Text = String.Format("{0:F4}", quaternion.w);
                                        CRState.Text = GetDeviceState(eTrackingResult);
                                        CRulPressed.Text = String.Format("{0}", controllerState.ulButtonPressed);
                                        CRulTouched.Text = String.Format("{0}", controllerState.ulButtonTouched);
                                        CRAxis0X.Text = String.Format("{0:F4}", controllerState.rAxis0.x);
                                        CRAxis0Y.Text = String.Format("{0:F4}", controllerState.rAxis0.y);
                                        CRAxis1X.Text = String.Format("{0:F4}", controllerState.rAxis1.x);
                                        CRAxis1Y.Text = String.Format("{0:F4}", controllerState.rAxis1.y);
                                        CRAxis2X.Text = String.Format("{0:F4}", controllerState.rAxis2.x);
                                        CRAxis2Y.Text = String.Format("{0:F4}", controllerState.rAxis2.y);
                                        CRAxis3X.Text = String.Format("{0:F4}", controllerState.rAxis3.x);
                                        CRAxis3Y.Text = String.Format("{0:F4}", controllerState.rAxis3.y);
                                        CRAxis4X.Text = String.Format("{0:F4}", controllerState.rAxis4.x);
                                        CRAxis4Y.Text = String.Format("{0:F4}", controllerState.rAxis4.y);
                                    }));
                                    break;
                            }
                            

                            break;
                        case ETrackedDeviceClass.GenericTracker:

                            system.GetControllerStateWithPose(ETrackingUniverseOrigin.TrackingUniverseStanding, unDevice, ref controllerState, (uint)Marshal.SizeOf(controllerState), ref TrackedDevicePose);
                            // get the position and rotation
                            position = GetPosition(TrackedDevicePose.mDeviceToAbsoluteTracking);
                            quaternion = GetRotation(TrackedDevicePose.mDeviceToAbsoluteTracking);

                            // for printing some more info to the user about the state of the device/pose
                            eTrackingResult = m_rTrackedDevicePose[unDevice].eTrackingResult;

                            // print the tracking data
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate {
                                TrackerIndex.Text = String.Format("{0}", unDevice);
                                TrackerVectorX.Text = String.Format("{0:F4}", position.v0);
                                TrackerVectorY.Text = String.Format("{0:F4}", position.v1);
                                TrackerVectorZ.Text = String.Format("{0:F4}", position.v2);
                                TrackerQuaternionX.Text = String.Format("{0:F4}", quaternion.x);
                                TrackerQuaternionY.Text = String.Format("{0:F4}", quaternion.y);
                                TrackerQuaternionZ.Text = String.Format("{0:F4}", quaternion.z);
                                TrackerQuaternionW.Text = String.Format("{0:F4}", quaternion.w);
                                TrackerState.Text = GetDeviceState(eTrackingResult);
                            }));
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            VRThread.Abort();
            OpenVR.Shutdown();
            Application.Current.Shutdown();
        }
    }
}
