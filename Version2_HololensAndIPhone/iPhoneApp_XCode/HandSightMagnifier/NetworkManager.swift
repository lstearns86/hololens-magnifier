//
//  NetworkManager.swift
//  HandSightMagnifier
//
//  Created by Lee Stearns on 2/7/18.
//  Copyright © 2018 Lee Stearns. All rights reserved.
//

import Foundation
import UIKit
import CocoaAsyncSocket

class NetworkManager : NSObject, NetServiceDelegate, NetServiceBrowserDelegate, GCDAsyncSocketDelegate {
    var browser:NetServiceBrowser!
    var service: NetService!
    var services = [NetService]()
    
    let commandPort:UInt16 = 8889
    let videoPort:UInt16 = 8899
    
    var commandConnected = false
    var videoConnected = false
    var commandSocket:GCDAsyncSocket?
    var videoSocket:GCDAsyncSocket?
    
    let lineSeparator = Data(bytes: Array("\n".utf8))
    
    public var fps = 10.0
    
    func start() {

        // find the hololens IP address (advertised as a bonjour service)
        browser = NetServiceBrowser()
        browser?.delegate = self
        browser?.searchForServices(ofType: "_hololens3._tcp", inDomain: "local.")
    }
    
    func restart() {
        commandConnected = false
        videoConnected = false
        if commandSocket != nil && commandSocket!.isConnected == true { commandSocket!.disconnect(); commandSocket = nil }
        if videoSocket != nil && videoSocket!.isConnected == true { videoSocket!.disconnect(); videoSocket = nil }
        
        if browser != nil { browser.stop() }
        browser = nil
        
        start()
    }
    
//    func socket(_ sock: GCDAsyncSocket, didRead data: Data, withTag tag: Int) {
//        print("Received data")
//    }
    
    func netServiceBrowser(_ browser: NetServiceBrowser, didFind service: NetService, moreComing: Bool) {
        print("Service appeared: \(service)")
        services.append(service)
        service.delegate = self
        service.resolve(withTimeout: 5.0)
    }

    func netService(_ sender: NetService, didNotResolve errorDict: [String : NSNumber]) {
        print("Error resolving service")
    }
    
    func netServiceDidResolveAddress(_ sender: NetService) {
        var hostname = [CChar](repeating: 0, count: Int(NI_MAXHOST))
        guard let data = sender.addresses?.first else { return }
        data.withUnsafeBytes { (pointer:UnsafePointer<sockaddr>) -> Void in
            guard getnameinfo(pointer, socklen_t(data.count), &hostname, socklen_t(hostname.count), nil, 0, NI_NUMERICHOST) == 0 else {
                return
            }
        }
        let ipAddress = String(cString:hostname)
        print(ipAddress)
        
        commandSocket = GCDAsyncSocket(delegate: self, delegateQueue: DispatchQueue.main)
        do {
            try commandSocket?.connect(toHost: ipAddress, onPort: commandPort, withTimeout: 5)
        } catch {
            print("Couldn't connect to command socket:")
            print(error)
        }
        
        videoSocket = GCDAsyncSocket(delegate: self, delegateQueue: DispatchQueue.main)
        do {
            try videoSocket?.connect(toHost: ipAddress, onPort: videoPort, withTimeout: 5)
        } catch {
            print("Couldn't connect to video socket:")
            print(error)
        }
    }
    
    func socketDidDisconnect(_ sock: GCDAsyncSocket, withError err: Error?) {
//        restart()
    }
    
    func socket(_ sock: GCDAsyncSocket, didConnectToHost host: String, port: UInt16) {
        if sock == commandSocket {
            print("Command socket connected to \(host)")
            commandConnected = true
//            commandSocket!.readData(toLength: 5, withTimeout: -1, tag: 0)
            sock.readData(to: lineSeparator, withTimeout: -1, tag: 0)
        } else if sock == videoSocket {
            print("Video socket connected to \(host)")
            videoConnected = true
        }
    }
    
    func socket(_ sock: GCDAsyncSocket, didRead data: Data, withTag tag: Int) {
        
        if let text = String(data: data, encoding: .utf8) {
            let components = text.trimmingCharacters(in: CharacterSet.newlines).split(separator: ",")
            if components.count == 5 {
                if let tempFps = Double(components[4]) {
                    if fps != tempFps { print("set fps to \(fps)") }
                    fps = tempFps
                }
            }
        
        }
//        commandSocket!.readData(toLength: 5, withTimeout: -1, tag: 0)
        sock.readData(to: lineSeparator, withTimeout: -1, tag: 0)
    }
    
    func send(text: String) {
        if(commandSocket != nil && commandSocket!.isConnected) {
            if let data = "\(text)\n".data(using: .utf8) {
                commandSocket?.write(data, withTimeout: -1, tag: 1)
            }
        }
    }
    
    func send(data: Data) {
        if(videoSocket != nil && videoSocket!.isConnected) {
            
            // send image size
            let size = Int32(data.count)
            write(socket: videoSocket, value: size, tag: 1)
            
            // send current time
            let calendar = Calendar.current
            let date = Date()
            let hour = Int32(calendar.component(.hour, from: date))
            let minute = Int32(calendar.component(.minute, from: date))
            let second = Int32(calendar.component(.second, from: date))
            let millisecond = Int32(Double(calendar.component(.nanosecond, from: date)) * 1e-6)
            write(socket: videoSocket, value: hour, tag: 2)
            write(socket: videoSocket, value: minute, tag: 3)
            write(socket: videoSocket, value: second, tag: 4)
            write(socket: videoSocket, value: millisecond, tag: 5)
            
//            print("\(hour):\(minute):\(second).\(millisecond)")
            
            // send data
            videoSocket?.write(data, withTimeout: -1, tag: 2)
            
//            print("Sent (\(size) bytes)")
        }
    }
    
    private func write(socket: GCDAsyncSocket?, value: Int32, tag: Int) {
        var tempValue = value
        let data = Data(buffer: UnsafeBufferPointer(start: &tempValue, count: 1))
        socket?.write(data, withTimeout: -1, tag: tag)
    }
}
