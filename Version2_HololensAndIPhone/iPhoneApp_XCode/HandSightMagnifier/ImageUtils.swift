//
//  ImageUtils.swift
//  HandSightMagnifier
//
//  Created by Lee Stearns on 2/13/18.
//  Copyright © 2018 Lee Stearns. All rights reserved.
//

import Foundation
import UIKit

// Image extension
extension UIImage {
    
    func rotateRight() -> UIImage? {
        var transform = CGAffineTransform.identity
        transform = transform.translatedBy(x: 0, y: self.size.width);
        transform = transform.rotated(by: CGFloat(-Double.pi / 2.0));
        
        if let context: CGContext = CGContext(data: nil, width: Int(self.size.height), height: Int(self.size.width),
                                              bitsPerComponent: self.cgImage!.bitsPerComponent, bytesPerRow: 0,
                                              space: self.cgImage!.colorSpace!,
                                              bitmapInfo: self.cgImage!.bitmapInfo.rawValue) {
            
            context.concatenate(transform)
            
            if ( self.imageOrientation == UIImageOrientation.left ||
                self.imageOrientation == UIImageOrientation.leftMirrored ||
                self.imageOrientation == UIImageOrientation.right ||
                self.imageOrientation == UIImageOrientation.rightMirrored ) {
                context.draw(self.cgImage!, in: CGRect(x: 0,y: 0,width: self.size.height,height: self.size.width))
            } else {
                context.draw(self.cgImage!, in: CGRect(x: 0,y: 0,width: self.size.width,height: self.size.height))
            }
            
            if let contextImage = context.makeImage() {
                return UIImage(cgImage: contextImage)
            }
            
        }
        
        return nil
    }
    
    func resize(width: CGFloat, height: CGFloat) -> UIImage {
        return resize(CGSize(width: width, height: height))
    }
    
    func resize(_ targetSize: CGSize) -> UIImage {
        let size = self.size
        
        let widthRatio  = targetSize.width  / size.width
        let heightRatio = targetSize.height / size.height
        
        // Figure out what our orientation is, and use that to form the rectangle
        var newSize: CGSize
        if(widthRatio > heightRatio) {
            newSize = CGSize(width: size.width * heightRatio, height: size.height * heightRatio)
        } else {
            newSize = CGSize(width: size.width * widthRatio, height: size.height * widthRatio)
        }
        
        // This is the rect that we've calculated out and this is what is actually used below
        let rect = CGRect(x: 0, y: 0, width: newSize.width, height: newSize.height)
        
        // Actually do the resizing to the rect using the ImageContext stuff
        UIGraphicsBeginImageContextWithOptions(newSize, false, 1.0)
        self.draw(in: rect)
        let newImage = UIGraphicsGetImageFromCurrentImageContext()
        UIGraphicsEndImageContext()
        
        return newImage!
    }
    
    func cropCenter(_ targetSize: CGSize) -> UIImage {
        
        let rect = CGRect(x: (self.size.width - targetSize.width) / 2, y: (self.size.height - targetSize.height) / 2, width: targetSize.width, height: targetSize.height)
        
        // slow, should find a better way to do it with cgImage.cropping
        UIGraphicsBeginImageContextWithOptions(rect.size, false, self.scale)
        self.draw(at: CGPoint(x: -rect.origin.x, y: -rect.origin.y))
        let croppedImg = UIGraphicsGetImageFromCurrentImageContext()
        UIGraphicsEndImageContext()
        
        return croppedImg!;
    }
    
    func pixelData() -> Data? {
        let size = self.size
        let dataSize = size.width * size.height * 4
        var pixelData = [UInt8](repeating: 0, count: Int(dataSize))
        let colorSpace = CGColorSpaceCreateDeviceRGB()
        let context = CGContext(data: &pixelData,
                                width: Int(size.width),
                                height: Int(size.height),
                                bitsPerComponent: 8,
                                bytesPerRow: 4 * Int(size.width),
                                space: colorSpace,
                                bitmapInfo: CGImageAlphaInfo.noneSkipLast.rawValue)
        guard let cgImage = self.cgImage else { return nil }
        context?.draw(cgImage, in: CGRect(x: 0, y: 0, width: size.width, height: size.height))
        
        return Data(bytes: pixelData)
    }
    
    func invert() -> UIImage {
        if let cgImg = self.cgImage {
            
            let img = CoreImage.CIImage(cgImage: cgImg)
            
            let filter = CustomThresholdFilter()
            filter.setDefaults()
            filter.inputImage = img
            
            let context = CIContext(options:nil)
            
            if let output = filter.outputImage {
                if let invertedImg = context.createCGImage(output, from: output.extent) {
                    return UIImage(cgImage: invertedImg)
                }
            }
        }
        
        print("Couldn't invert image")
        return self
    }
}
