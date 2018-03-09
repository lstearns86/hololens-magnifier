//
//  ViewController.swift
//  HandSightMagnifier
//
//  Created by Lee Stearns on 2/6/18.
//  Copyright © 2018 Lee Stearns. All rights reserved.
//

import UIKit
import SceneKit
import ARKit
import AVFoundation

class ViewController: UIViewController, ARSCNViewDelegate, ARSessionDelegate {

    @IBOutlet var sceneView: ARSCNView!
    @IBOutlet weak var waitingIndicator: UIActivityIndicatorView!
    @IBOutlet weak var calibrationPattern: UIImageView!
    @IBOutlet weak var modeOverlay: UIView!
    @IBOutlet weak var debugOverlay: UIView!
    @IBOutlet weak var processedImageView: UIImageView!
    
    // gesture recognizers
    @IBOutlet var twoFingerDoubleTapGestureRecognizer: UITapGestureRecognizer!
    @IBOutlet var oneFingerSingleTapGestureRecognizer: UITapGestureRecognizer!
    var longOrFirmPressGestureRecognizer: LongOrFirmPressGestureRecognizer!
    
    // buttons
    @IBOutlet weak var selfButton: UIButton!
    @IBOutlet weak var worldButton: UIButton!
    @IBOutlet weak var phoneButton: UIButton!
    @IBOutlet weak var lightButton: UIButton!
    @IBOutlet weak var invertColorsButton: UIButton!
    
    var net: NetworkManager!
    var calibrating = false, panning = false, invert = false
    let frameDelay = 1.0 / 8.0 // in seconds
    var lastFrameSent = Date()
    var previousScale = 1.0
    
    let lightVibration = UIImpactFeedbackGenerator(style: .light)
    let mediumVibration = UIImpactFeedbackGenerator(style: .medium)
    let heavyVibration = UIImpactFeedbackGenerator(style: .heavy)
    
    var sendHighResolutionNextFrame = false
    var videoPaused = false
    var showProcessedImage = false
    
    var captureSession: AVCaptureSession?
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Set the view's delegate
        sceneView.delegate = self
        
        // Show statistics such as fps and timing information
        sceneView.showsStatistics = false
        
        // Create a new scene
        let scene = SCNScene()
        
        // Set the scene to the view
        sceneView.scene = scene
        sceneView.session.delegate = self
        
        NotificationCenter.default.addObserver(self, selector: #selector(rotate), name: NSNotification.Name.UIDeviceOrientationDidChange, object: nil)
        
        longOrFirmPressGestureRecognizer = LongOrFirmPressGestureRecognizer(target: self, action: #selector(longOrFirmPressGestureRecognized(_:)), durationThreshold: 1, pressureThreshold: 0.75)
        longOrFirmPressGestureRecognizer.isEnabled = true
        view.addGestureRecognizer(longOrFirmPressGestureRecognizer)
//        longOrFirmPressGestureRecognizer.durationThreshold = 1
//        longOrFirmPressGestureRecognizer.pressureThreshold = 0.5
        
        // uncomment these two lines to debug image processing (will overlay on screen)
//        processedImageView.isHidden = false
//        showProcessedImage = true
        
        // Set up still image variables
        // TODO?
        
        // Set up networking
        net = NetworkManager()
        net.start()
    }
    
    @objc func rotate() {
        var transform: CGAffineTransform
        switch UIDevice.current.orientation {
        case .portraitUpsideDown: transform = CGAffineTransform(rotationAngle: CGFloat.pi); break
        case .landscapeLeft: transform = CGAffineTransform(rotationAngle: CGFloat.pi / 2); break
        case .landscapeRight: transform = CGAffineTransform(rotationAngle: -CGFloat.pi / 2); break
        default: transform = CGAffineTransform.identity
        }
        
        UIView.animate(withDuration:0.3, animations: {
            self.selfButton.transform = transform
            self.worldButton.transform = transform
            self.phoneButton.transform = transform
            self.lightButton.transform = transform
            self.invertColorsButton.transform = transform
        })
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        
        // Create a session configuration
        let configuration = ARWorldTrackingConfiguration()

        // in iOS 11.3 (currently beta)
        configuration.isAutoFocusEnabled = true
        configuration.worldAlignment = .gravity

        // Run the view's session
        sceneView.session.run(configuration)
        
        // Start (or restart) networking
//        net.restart()
    }
    
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        // Pause the view's session
        sceneView.session.pause()
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Release any cached data, images, etc that aren't in use.
    }

    // MARK: - ARSCNViewDelegate
    
/*
    // Override to create and configure nodes for anchors added to the view's session.
    func renderer(_ renderer: SCNSceneRenderer, nodeFor anchor: ARAnchor) -> SCNNode? {
        let node = SCNNode()
     
        return node
    }
*/
    
    func session(_ session: ARSession, didFailWithError error: Error) {
        // Present an error message to the user
        
    }
    
    func sessionWasInterrupted(_ session: ARSession) {
        // Inform the user that the session has been interrupted, for example, by presenting an overlay
        print("Session interrupted")
    }
    
    func sessionShouldAttemptRelocalization(_ session: ARSession) -> Bool {
        return false
    }
    
    func sessionInterruptionEnded(_ session: ARSession) {
        // Reset tracking and/or remove existing anchors if consistent tracking is required
        print("Session resumed")
    }
    
    func session(_ session: ARSession, didUpdate frame: ARFrame) {
        
        let now = Date()
        let elapsed = now.timeIntervalSince(lastFrameSent)
        if elapsed > 1.0 / net.fps && !videoPaused {
//            DispatchQueue.global(qos: .userInteractive).async {
//                let start = Date()
                let ciImg = CIImage(cvPixelBuffer: frame.capturedImage)
                if let cgImage = CIContext(options: nil).createCGImage(ciImg, from: ciImg.extent) {
                    if sendHighResolutionNextFrame {
                        if let uiImgRaw = UIImage(cgImage: cgImage).rotateRight() {
                            let uiImg = invert ? uiImgRaw.invert() : uiImgRaw
                            if showProcessedImage { processedImageView.image = uiImg }
                            if let data = UIImageJPEGRepresentation(uiImg, 100) {
                                self.net.send(data: data)
                                sendHighResolutionNextFrame = false
                                videoPaused = true
                            }
                        }
                    } else {
                        if let uiImgRaw = UIImage(cgImage: cgImage).rotateRight()?.resize(width: 432, height: 768) {
                            let uiImg = invert ? uiImgRaw.invert() : uiImgRaw
                            if showProcessedImage { processedImageView.image = uiImg }
                            if let data = UIImageJPEGRepresentation(uiImg, 100) {
                                self.net.send(data: data)
                            }
                        }
                    }
//                }
//                let elapsed = Date().timeIntervalSince(start)
//                print("Elapsed: \(elapsed)")
                self.lastFrameSent = now
            }
        }
        
        let state = frame.camera.trackingState
        switch state {
        case .normal:
            waitingIndicator.isHidden = true
            break
        case .notAvailable:
            waitingIndicator.isHidden = false
//            print("Tracking not available")
            break
        case .limited(let reason):
            waitingIndicator.isHidden = false
            switch reason {
            case .excessiveMotion:
//                print("Tracking limited, excessive motion")
                break
            case .insufficientFeatures:
//                print("Tracking limited, insufficient features")
                break
            case .initializing:
//                print("Tracking limited, initializing")
                break
            case .relocalizing:
//                print("Trackingn limited, relocalizing")
                break
            }
            break
        }
        
        let transform = frame.camera.transform
        net.send(text: "{transform:[\(transform.columns.0.x),\(transform.columns.1.x),\(transform.columns.2.x),\(transform.columns.3.x);\(transform.columns.0.y),\(transform.columns.1.y),\(transform.columns.2.y),\(transform.columns.3.y);\(transform.columns.0.z),\(transform.columns.1.z),\(transform.columns.2.z),\(transform.columns.3.z)]}")
    }
    
    @IBAction func twoFingerDoubleTapGestureRecognized(_ sender: Any) {
        
//        print("Two finger double tap")
        
        debugOverlay.isHidden = false
    }
    
    @IBAction func oneFingerTapGestureRecognized(_ sender: Any) {
        
//        print("One finger single tap")
        
        if calibrating {
            net.send(text: "{calibration: store}")
        } else {
            // capture hi-res image
//            sceneView.session.pause()
//            // TODO
//            sceneView.session.run(sceneView.session.configuration!)
        }
    }
    
    @IBAction func pinchGestureRecognized(_ sender: UIPinchGestureRecognizer) {
        
        if sender.state == .began { previousScale = 1.0 }
        let scaleDelta = sender.scale / CGFloat(previousScale)
        previousScale = Double(sender.scale)
        
        net.send(text: "{scale: \(scaleDelta)}")
    }
    
    @IBAction func panGestureRecognized(_ sender: UIPanGestureRecognizer) {
        
        let translation = sender.translation(in: view)
        net.send(text: "{pan: {x: \(translation.x), y: \(translation.y), touches: \(sender.numberOfTouches)}}")
        sender.setTranslation(CGPoint.zero, in: view)
    }
    
    @IBAction func doubleTapGestureRecognized(_ sender: UITapGestureRecognizer) {
        modeOverlay.isHidden = false
    }
    
    @IBAction func longOrFirmPressGestureRecognized(_ sender: LongOrFirmPressGestureRecognizer) {
        if sender.state == .began {
            net.send(text: "{positioning: start}")
            heavyVibration.impactOccurred()
        } else if sender.state == .ended {
            net.send(text: "{positioning: stop}")
            lightVibration.impactOccurred()
        }
    }
    
    @IBAction func attachToSelfButtonPressed(_ sender: Any) {
        net.send(text: "{attach: self}")
        modeOverlay.isHidden = true
    }
    
    @IBAction func attachToWorldButtonPressed(_ sender: Any) {
        net.send(text: "{attach: world}")
        modeOverlay.isHidden = true
    }
    
    @IBAction func attachToPhoneButtonPressed(_ sender: Any) {
        net.send(text: "{attach: phone}")
        modeOverlay.isHidden = true
    }
    
    @IBAction func calibrateButtonPressed(_ sender: Any) {
        net.send(text: "{calibration: start}")
        calibrating = true
        calibrationPattern.isHidden = false
        debugOverlay.isHidden = true
    }
    
    @IBAction func fineTuneButtonPressed(_ sender: Any) {
        net.send(text: "{calibration: fine}")
        calibrating = false
        calibrationPattern.isHidden = true
        debugOverlay.isHidden = true
    }
    
    @IBAction func stopCalibrationButtonPressed(_ sender: Any) {
        net.send(text: "{calibration: stop}")
        calibrating = false
        calibrationPattern.isHidden = true
        debugOverlay.isHidden = true
    }
    
    @IBAction func lightButtonPressed(_ sender: UIButton) {
        lightButton.isSelected = !lightButton.isSelected
        toggleTorch(on: lightButton.isSelected)
    }
    
    func toggleTorch(on: Bool) {
        guard let device = AVCaptureDevice.default(for: AVMediaType.video)
            else {return}
        
        if device.hasTorch {
            do {
                try device.lockForConfiguration()
                
                if on == true {
                    device.torchMode = .on
                } else {
                    device.torchMode = .off
                }
                
                device.unlockForConfiguration()
            } catch {
                print("Torch could not be used")
            }
        } else {
            print("Torch is not available")
        }
    }
    
    @IBAction func invertColorsButtonPressed(_ sender: UIButton) {
        invertColorsButton.isSelected = !invertColorsButton.isSelected
        invert = invertColorsButton.isSelected
    }
    
    @IBAction func resetTrackingButtonPressed(_ sender: UIButton) {
        
        sceneView.session.pause()
        
        sceneView.session.run(sceneView.session.configuration!, options: .resetTracking)
        
    }
}
