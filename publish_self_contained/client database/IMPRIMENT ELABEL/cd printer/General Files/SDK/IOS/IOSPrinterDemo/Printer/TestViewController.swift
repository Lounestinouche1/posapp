//
//  TestViewController.swift
//  Printer
//
//  Created by ding on 2021/4/29.
//  Copyright © 2021 Admin. All rights reserved.
//

import UIKit

class TestViewController: UIViewController {
    var test:PrinterManager!
    var test2:PrinterManager!
    var isConnectedGlobal:Bool=false
    override func viewDidLoad() {
        super.viewDidLoad()
        //ConnectPrinter();
    }
    override func viewWillDisappear(_ animated: Bool) {
        test.disConnectPrinter();
    }
    func ConnectPrinter(){
        let queue = DispatchQueue(label: "com.receipt.printer1", qos: .default, attributes: .concurrent, autoreleaseFrequency: .inherit, target: nil)
        queue.async {
            var isConnected:Bool;
            self.test=PrinterManager.share(0,threadID: "com.receipt.printer1");
            isConnected=self.test.connectWifiPrinter("192.168.3.54", port: 9100);
            self.isConnectedGlobal=isConnected;
            if(isConnected){
                print("connect printer1 succeessfully\n");
                self.test.startMonitor();
            }else{
                print("connect printer1 failed\n");
            }
        }
//        let queue2 = DispatchQueue(label: "com.receipt.printer2", qos: .default, attributes: .concurrent, autoreleaseFrequency: .inherit, target: nil)
//        queue2.async {
//            var isConnected:Bool;
//            self.test2=PrinterManager.share(0,threadID: "com.receipt.printer2");
//            isConnected=self.test2.connectWifiPrinter("192.168.3.88", port: 9100);
//            if(isConnected){
//                print("connect printer2 succeessfully\n");
//                self.test2.startMonitor();
//            }else{
//                print("connect printer2 failed\n");
//            }
//        }
    }
    @IBAction func PrintTest(_ sender: Any) {
        for i in 0...4{
        if(!self.isConnectedGlobal){
            ConnectPrinter();
        }
        sleep(2);
        if(self.isConnectedGlobal){
            let queue = DispatchQueue(label: "com.receipt.printer1", qos: .default, attributes: .concurrent, autoreleaseFrequency: .inherit, target: nil)
            queue.async {
                var printSucceed:Bool;
                //printSucceed=test.sendData(toPrinter:Data("hello swift\n".utf8));
                let image=UIImage(named: "test.jpeg");
                let imgData:Data=PosCommand.printRasteBmp(withM: RasterNolmorWH, andImage: image, andType: Dithering);
                printSucceed=self.test.sendReceipt(toPrinter: imgData);
                if(printSucceed){
                    print("print1 succeessfully\n");
                }else{
                    print("print1 failed\n");
                    print(self.test.getPrinterStatus());
                }
                self.test.disConnectPrinter();
                self.isConnectedGlobal=false;
            }
        }else{
            print("printer offline or printer error");
        }
//        let queue2 = DispatchQueue(label: "com.receipt.printer2", qos: .default, attributes: .concurrent, autoreleaseFrequency: .inherit, target: nil)
//        queue2.async {
//            var printSucceed:Bool;
//            //printSucceed=test.sendData(toPrinter:Data("hello swift\n".utf8));
//            let image=UIImage(named: "test.jpeg");
//            let imgData:Data=PosCommand.printRasteBmp(withM: RasterNolmorWH, andImage: image, andType: Dithering);
//            printSucceed=self.test2.sendReceipt(toPrinter: imgData);
//            if(printSucceed){
//                print("print2 succeessfully\n");
//            }else{
//                print("print2 failed\n");
//                print(self.test2.getPrinterStatus());
//            }
//        }
        }
    }
}
