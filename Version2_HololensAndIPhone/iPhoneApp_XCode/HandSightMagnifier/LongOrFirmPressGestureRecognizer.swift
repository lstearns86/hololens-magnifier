//
//  LongOrFirmPressGestureRecognizer.swift
//  HandSightMagnifier
//
//  Created by Lee Stearns on 2/27/18.
//  Copyright © 2018 Lee Stearns. All rights reserved.
//

import UIKit.UIGestureRecognizerSubclass

class LongOrFirmPressGestureRecognizer : UIGestureRecognizer
{
    var durationThreshold: CGFloat
    var pressureThreshold: CGFloat
    
    var triggered = false
    var touchStart = Date()
    
    required init(target: AnyObject?, action: Selector, durationThreshold: CGFloat, pressureThreshold: CGFloat) {
        self.durationThreshold = durationThreshold
        self.pressureThreshold = pressureThreshold
        super.init(target: target, action: action)
    }
    
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent) {
        if touches.count > 1 { state = .failed; return }
        
        if let touch = touches.first {
            touchStart = Date()
            handleTouch(touch: touch)
        }
    }
    
    override func touchesMoved(_ touches: Set<UITouch>, with event: UIEvent) {
        if touches.count > 1 || state == .failed { state = .failed; return }
        if let touch = touches.first {
            handleTouch(touch: touch)
        }
    }
    
    override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent) {
        super.touchesEnded(touches, with: event)
        state = triggered && state != .failed ? .ended : .failed
        triggered = false
    }
    
    private func handleTouch(touch: UITouch)
    {
        if touch.maximumPossibleForce == 0 {
            return
        }
        
        let elapsed = CGFloat(Date().timeIntervalSince(touchStart))
        
        if !triggered && (touch.force / touch.maximumPossibleForce > pressureThreshold || elapsed > durationThreshold) {
//            print("triggered: \((touch.force / touch.maximumPossibleForce > pressureThreshold) ? "force" : "duration")")
            state = .began
            triggered = true
        }
    }
}
