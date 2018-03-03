//
//  CustomThresholdFilter.swift
//  HandSightMagnifier
//
//  Created by Lee Stearns on 3/2/18.
//  Copyright © 2018 Lee Stearns. All rights reserved.
//

import UIKit
import CoreGraphics

class CustomThresholdFilter: CIFilter {
    var inputImage: CIImage?
    
    var thresholdKernel =  CIColorKernel(source:
        "kernel vec4 thresholdFilter(__sample image, __sample threshold)" +
            "{" +
            "   float imageLuma = dot(image.rgb, vec3(0.2126, 0.7152, 0.0722));" +
            "   float thresholdLuma = dot(threshold.rgb, vec3(0.2126, 0.7152, 0.0722));" +
            
            "   return vec4(vec3(step(imageLuma, thresholdLuma-0.1)), 1.0);" +
        "}")
    
    override public var outputImage: CIImage? {
        get {
            guard let inputImage = inputImage,
                let thresholdKernel = thresholdKernel else
            {
                return nil
            }
            
            let blurred = inputImage.applyingFilter("CIBoxBlur",
                                                    parameters: [kCIInputRadiusKey: 31])
            
            let args = [inputImage, blurred]
            return thresholdKernel.apply(extent: inputImage.extent, arguments: args)
        }
    }
}
