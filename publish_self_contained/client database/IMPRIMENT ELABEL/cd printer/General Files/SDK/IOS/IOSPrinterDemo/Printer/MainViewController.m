//
//  MainViewController.m
//  Printer
//
//  Created by femto01 on 16/4/29.
//  Copyright © 2016年 Admin. All rights reserved.
//

#import "MainViewController.h"
#import "POSSDK.h"
#import "ViewController.h"

@interface MainViewController () <POSBLEManagerDelegate, POSWIFIManagerDelegate>

/** BLE */
@property (strong, nonatomic) POSBLEManager *manager;

/** wifi */
@property (nonatomic, strong) POSWIFIManager *wifiManager;

@property (weak, nonatomic) IBOutlet UISwitch *blueToothModel;

/** 连接状态指示 label */
@property (weak, nonatomic) IBOutlet UILabel *connectState;

/** 跳到扫描蓝牙设备的按钮 */
@property (weak, nonatomic) IBOutlet UIButton *scnaBlueToothButton;
@property (weak, nonatomic) IBOutlet UITextField *IPAddressTextField;
@property (weak, nonatomic) IBOutlet UITextField *partNumberTextField;

@end

@implementation MainViewController

- (POSWIFIManager *)wifiManager
{
    if (!_wifiManager)
    {
        _wifiManager = [POSWIFIManager shareWifiManager];
        _wifiManager.delegate = self;
    }
    return _wifiManager;
}

- (POSBLEManager *)manager
{
    if (!_manager)
    {
        _manager = [POSBLEManager sharedInstance];
        _manager.delegate = self;
        [_manager addObserver:self
                   forKeyPath:@"writePeripheral.state"
                      options:NSKeyValueObservingOptionNew | NSKeyValueObservingOptionOld
                      context:nil];
    }
    
    return _manager;
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSString *,id> *)change context:(void *)context
{
    if (object == self.manager && [keyPath isEqualToString:@"writePeripheral.state"])
    {
        // 更行蓝牙的连接状态
        switch (self.manager.writePeripheral.state) {
            case CBPeripheralStateDisconnected:
            {
                self.connectState.text = @"Disconnected"; //
                break;
            }
                
            case CBPeripheralStateConnecting:
            {
                self.connectState.text = @"Connecting"; //
                break;
            }
                
            case CBPeripheralStateConnected:
            {
                self.connectState.text = @"Connected"; //
                break;
            }
                
            case CBPeripheralStateDisconnecting:
            {
                self.connectState.text = @"Disconnecting"; //
                break;
            }
                
            default:
                break;
        }
        
        ;
    }
}

- (void)viewDidLoad {
    [super viewDidLoad];
    [self manager];
    [self wifiManager];
    
    self.IPAddressTextField.text = @"192.168.3.99";
    self.partNumberTextField.text = @"9100";
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
    // Dispose of any resources that can be recreated.
}


- (void)prepareForSegue:(UIStoryboardSegue *)segue sender:(id)sender {
    
    [super prepareForSegue:segue sender:sender];
}

#pragma mark - WIFIManagerDelegate
/**
 连接上主机
 */
- (void)POSWIFIManager:(POSWIFIManager *)manager didConnectedToHost:(NSString *)host port:(UInt16)port {
    if (!manager.isAutoDisconnect) {
//        self.myTab.hidden = NO;
    }
    [MBProgressHUD showSuccess:@"connect success" toView:self.view];
}
/**
 读取到服务器的数据
 */
- (void)POSWIFIManager:(POSWIFIManager *)manager didReadData:(NSData *)data tag:(long)tag {
    
}
/**
 写数据成功
 */
- (void)POSWIFIManager:(POSWIFIManager *)manager didWriteDataWithTag:(long)tag {
    NSLog(@"write success");
}

/**
 断开连接
 */
- (void)POSWIFIManager:(POSWIFIManager *)manager willDisconnectWithError:(NSError *)error {}

- (void)POSWIFIManagerDidDisconnected:(POSWIFIManager *)manager {
    
    if (!manager.isAutoDisconnect) {
//        self.myTab.hidden = YES;
    }
    
    
    NSLog(@"PosWIFIManagerDidDisconnected");
    
}


#pragma mark - PosBLEDelegate
- (void)POSdidUpdatePeripheralList:(NSArray *)peripherals RSSIList:(NSArray *)rssiList{
    if ([self.navigationController.topViewController isKindOfClass:[ViewController class]]) {
        ViewController *VC = (ViewController *)self.navigationController.topViewController;
        [VC PosdidUpdatePeripheralList:peripherals RSSIList:rssiList];
    }
}

/** 连接成功 */
- (void)POSdidConnectPeripheral:(CBPeripheral *)peripheral{
    if ([self.navigationController.topViewController isKindOfClass:[ViewController class]]) {
        ViewController *VC = (ViewController *)self.navigationController.topViewController;
        [VC PosdidConnectPeripheral:peripheral];
    }
    NSString *mess = [NSString stringWithFormat:@"已经连接到蓝牙设备:\"%@\"", peripheral.name];
    NSLog(@"%@",mess);
    [MBProgressHUD showSuccess:mess toView:self.view];
}

// 连接失败
- (void)POSdidFailToConnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {
    if ([self.navigationController.topViewController isKindOfClass:[ViewController class]]) {
        ViewController *VC = (ViewController *)self.navigationController.topViewController;
        [VC PosdidFailToConnectPeripheral:peripheral error:error];
    }
    [MBProgressHUD hideHUDForView:self.view animated:NO];
    [MBProgressHUD showError:@"连接失败" toView:self.view];
}

// 写入数据成功
- (void)POSdidWriteValueForCharacteristic:(CBCharacteristic *)character error:(NSError *)error {
    if ([self.navigationController.topViewController isKindOfClass:[ViewController class]]) {
        ViewController *VC = (ViewController *)self.navigationController.topViewController;
        [VC PosdidWriteValueForCharacteristic:character error:error];
    }
    
}
// 断开连接
- (void)POSdidDisconnectPeripheral:(CBPeripheral *)peripheral isAutoDisconnect:(BOOL)isAutoDisconnect{
    if ([self.navigationController.topViewController isKindOfClass:[ViewController class]]) {
        ViewController *VC = (ViewController *)self.navigationController.topViewController;
        [VC PosdidDisconnectPeripheral:peripheral isAutoDisconnect:isAutoDisconnect];
    }
    
    if (isAutoDisconnect) {
        NSLog(@"自动断开...");
//        [self.navigationController popToViewController:self animated:YES];
        [[[UIAlertView alloc] initWithTitle:@"device disconnect" message:nil delegate:nil cancelButtonTitle:@"OK" otherButtonTitles: nil] show];
//        [self scanAgain:nil];
    }else {
        NSLog(@"手动断开...");
    }
    
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(1.0 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        [MBProgressHUD hideHUDForView:self.view animated:NO];
    });
    
    NSLog(@"%s",__func__);
}

#pragma mark - 按钮点击事件
/** 点击了 switch 开关 */
- (IBAction)changeModel:(id)sender {
    
    if ([self.blueToothModel isOn])
    {
//        NSLog(@"转蓝牙模式");
        [self.wifiManager POSDisConnect];
    }
    else
    {
//        NSLog(@"转Wifi模式");
        [self.manager POSdisconnectRootPeripheral];
    }
}
/** 隐藏键盘 */
- (IBAction)hideKeyboardClick:(id)sender {
    
    [self.view endEditing:YES];
}

/** 点击连接wifi按钮 */
- (IBAction)connectWifiClick:(id)sender {
    
    // 先断开原来的连接
    [self.wifiManager POSDisConnect];
    
    // 连接到 wifi
    [self.wifiManager POSConnectWithHost:self.IPAddressTextField.text
                                   port:(UInt16)[self.partNumberTextField.text integerValue]
                             completion:^(BOOL isConnect) {
                                 if (isConnect)
                                 {
                                     
                                 }
                                 else
                                 {
                                     
                                 }
                             }];
}

#pragma mark - Label Model
/** Label Model 上的 text 按钮 */
- (IBAction)labelTextClick:(id)sender {
    NSMutableData *dataM=[[NSMutableData alloc]init];
    NSData *data=[[NSData alloc]init];
    //    data=[self.codeTextField.text dataUsingEncoding:NSASCIIStringEncoding];
    data=[TscCommand sizeBymmWithWidth:75 andHeight:30];
    [dataM appendData:data];
    data=[TscCommand gapBymmWithWidth:3 andHeight:0];
    [dataM appendData:data];
    data=[TscCommand cls];
    [dataM appendData:data];
    data=[TscCommand textWithX:0 andY:0 andFont:@"TSS24.BF2" andRotation:0 andX_mul:1 andY_mul:1 andContent:@"12345678abcd" usStrEnCoding:NSASCIIStringEncoding];
    [dataM appendData:data];
    data=[TscCommand print:1];
    [dataM appendData:data];
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else
    {
        [self.manager POSWriteCommandWithData:dataM];
    }
}
//指定宽度按比例缩放，缩放有些时候只对宽度进行了缩放
-(UIImage *) imageCompressForWidthScale:(UIImage *)sourceImage targetWidth:(CGFloat)defineWidth{
    
    UIImage *newImage = nil;
    CGSize imageSize = sourceImage.size;
    CGFloat width = imageSize.width;
    CGFloat height = imageSize.height;
    CGFloat targetWidth = defineWidth;
    CGFloat targetHeight = height / (width / targetWidth);
    CGSize size = CGSizeMake(targetWidth, targetHeight);
    CGFloat scaleFactor = 0.0;
    CGFloat scaledWidth = targetWidth;
    CGFloat scaledHeight = targetHeight;
    CGPoint thumbnailPoint = CGPointMake(0.0, 0.0);
    
    if(CGSizeEqualToSize(imageSize, size) == NO){
        
        CGFloat widthFactor = targetWidth / width;
        CGFloat heightFactor = targetHeight / height;
        
        if(widthFactor > heightFactor){
            scaleFactor = widthFactor;
        }
        else{
            scaleFactor = heightFactor;
        }
        scaledWidth = width * scaleFactor;
        scaledHeight = height * scaleFactor;
        
        if(widthFactor > heightFactor){
            
            thumbnailPoint.y = (targetHeight - scaledHeight) * 0.5;
            
        }else if(widthFactor < heightFactor){
            
            thumbnailPoint.x = (targetWidth - scaledWidth) * 0.5;
        }
    }
    
    UIGraphicsBeginImageContext(size);
    
    CGRect thumbnailRect = CGRectZero;
    thumbnailRect.origin = thumbnailPoint;
    thumbnailRect.size.width = scaledWidth;
    thumbnailRect.size.height = scaledHeight;
    
    [sourceImage drawInRect:thumbnailRect];
    
    newImage = UIGraphicsGetImageFromCurrentImageContext();
    
    if(newImage == nil){
        
        NSLog(@"scale image fail");
    }
    UIGraphicsEndImageContext();
    return newImage;
}

/** Label Model 上的 Picture 按钮 */
- (IBAction)labelPictureClick:(id)sender {
    UIImage* image=[self imageCompressForWidthScale:[UIImage imageNamed:@"test.jpg"]  targetWidth:75];
    NSMutableData *dataM=[[NSMutableData alloc]init];
    NSData *data=[[NSData alloc]init];
    //    data=[self.codeTextField.text dataUsingEncoding:NSASCIIStringEncoding];
    data=[TscCommand sizeBymmWithWidth:75 andHeight:30];
    [dataM appendData:data];
    data=[TscCommand gapBymmWithWidth:3 andHeight:0];
    [dataM appendData:data];
    data=[TscCommand cls];
    [dataM appendData:data];
    data=[TscCommand bitmapWithX:10 andY:10 andMode:0 andImage:image andBmpType:Dithering];
    [dataM appendData:data];
    data=[TscCommand print:1];
    [dataM appendData:data];
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else
    {
        [self.manager POSWriteCommandWithData:dataM];
    }
}

/** Label Model 上的 QRCode 按钮 */
- (IBAction)labelQRCodeClick:(id)sender {
    NSMutableData *dataM=[[NSMutableData alloc]init];
    NSData *data=[[NSData alloc]init];
//    data=[self.codeTextField.text dataUsingEncoding:NSASCIIStringEncoding];
    data=[TscCommand sizeBymmWithWidth:58 andHeight:60];
    [dataM appendData:data];
    data=[TscCommand gapBymmWithWidth:3 andHeight:0];
    [dataM appendData:data];
    data=[TscCommand cls];
    [dataM appendData:data];
    data=[TscCommand qrCodeWithX:0 andY:0 andEccLevel:@"H" andCellWidth:4 andMode:@"A" andRotation:0 andContent:@"www.printer.com" usStrEnCoding:NSASCIIStringEncoding];
    [dataM appendData:data];
    data=[TscCommand print:1];
    [dataM appendData:data];
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else
    {
        [self.manager POSWriteCommandWithData:dataM];
    }
   
}

/** Label Model 上的 BarCode 按钮 */
- (IBAction)labelBarCodeClick:(id)sender {
    NSMutableData *dataM=[[NSMutableData alloc] init];
    NSData *data=[[NSData alloc] init];
    data=[TscCommand sizeBymmWithWidth:75 andHeight:30];
    [dataM appendData:data];
    data=[TscCommand gapBymmWithWidth:3 andHeight:0];
    [dataM appendData:data];
    data=[TscCommand cls];
    [dataM appendData:data];
    data=[TscCommand barcodeWithX:0 andY:0 andCodeType:@"128" andHeight:100 andHunabReadable:1 andRotation:0 andNarrow:2 andWide:6 andContent:@"012345678" usStrEnCoding:NSASCIIStringEncoding];
    [dataM appendData:data];
    data=[TscCommand print:1];
    [dataM appendData:data];
    NSLog(@"%@",dataM);
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else
    {
        [self.manager POSWriteCommandWithData:dataM];
    }
}

#pragma mark - Receipt Model


/** Receipt Model 上的 text 按钮 */
- (IBAction)receptTextClick:(id)sender {
    NSMutableData* dataM=[NSMutableData dataWithData:[PosCommand initializePrinter]];
    NSData* data=[@"helloworld1234567890123456789\n" dataUsingEncoding:NSASCIIStringEncoding];
    [dataM appendData:data];
    if (_blueToothModel.on==false)
    {
        NSLog(@"%@",dataM);
        [self.wifiManager POSWriteCommandWithData:dataM];
    
}
else
{
    [self.manager POSWriteCommandWithData:data];
}
}

/** Receipt Model 上的 Picture 按钮 */
- (IBAction)receiptPictureClick:(id)sender {
//    for (int i=0;i<10;i++) {
    UIImage* image=[UIImage imageNamed:@"test.jpeg"];
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:[PosCommand printRasteBmpWithM:RasterNolmorWH andImage:image andType:Dithering]];
        [self.wifiManager POSWriteCommandWithData:[PosCommand selectCutPageModelAndCutpage:0]];
    }
    else
    {
        [self.manager POSWriteCommandWithData:[PosCommand printRasteBmpWithM:RasterNolmorWH andImage:image andType:Dithering]];
        [self.manager POSWriteCommandWithData:[PosCommand selectCutPageModelAndCutpage:0]];
    }
//    }
}

/** Receipt Model 上的 QRCode 按钮 */
- (IBAction)receiptQRCodeClick:(id)sender {
    
    NSMutableData* dataM=[NSMutableData dataWithData:[PosCommand initializePrinter]];
    [dataM appendData:[PosCommand setQRcodeUnitsize:3]];
    [dataM appendData:[PosCommand setErrorCorrectionLevelForQrcode:48]];
    [dataM appendData:[PosCommand sendDataToStoreAreaWitQrcodeConent:@"wwwwwww" usEnCoding:CFStringConvertEncodingToNSStringEncoding(kCFStringEncodingGB_18030_2000)]];
    [dataM appendData:[PosCommand printTheQRcodeInStore]];
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else
    {
        [self.manager POSWriteCommandWithData:dataM];
    }


    
}

/** Receipt Model 上的 BarCode 按钮 */
- (IBAction)receiptBarCodeClick:(id)sender {
    NSMutableData* dataM=[NSMutableData dataWithData:[PosCommand initializePrinter]];
    [dataM appendData:[PosCommand selectAlignment:1]];
    [dataM appendData:[PosCommand selectHRICharactersPrintPosition:2]];
    [dataM appendData:[PosCommand setBarcoeWidth:3]];
    [dataM appendData:[PosCommand setBarcodeHeight:163]];
    [dataM appendData:[PosCommand printBarcodeWithM:65 andN:11 andContent:@"01234567890" useEnCodeing:NSASCIIStringEncoding]];
    if (_blueToothModel.on==false)
    {
        NSLog(@"%@",dataM);
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else{
        [self.manager POSWriteCommandWithData:dataM];
    }
}

/** Receipt Model 上的 Code128ABC 按钮 */
- (IBAction)receiptCode128ABCClick:(id)sender {
    NSMutableData* dataM=[NSMutableData dataWithData:[PosCommand initializePrinter]];
    [dataM appendData:[PosCommand selectAlignment:1]];
    [dataM appendData:[PosCommand selectHRICharactersPrintPosition:2]];
    [dataM appendData:[PosCommand setBarcoeWidth:3]];
    [dataM appendData:[PosCommand setBarcodeHeight:163]];
    [dataM appendData:[PosCommand printBarcodeWithM:73 andN:8 andContent:@"{Aabc123" useEnCodeing:NSASCIIStringEncoding]];
    [dataM appendData:[PosCommand printAndFeedLine]];
    if (_blueToothModel.on==false)
    {
        [self.wifiManager POSWriteCommandWithData:dataM];
    }
    else{
        [self.manager POSWriteCommandWithData:dataM];
    }
    
    
}

@end
