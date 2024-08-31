﻿| 公司名称                                                     | 型号                                                         | 通信接口     | 通信参数      | 流控 | 协议                                  | 拆包机制          | 校验方案                                                     |
| ------------------------------------------------------------ | ------------------------------------------------------------ | ------------ | ------------- | ---- | ------------------------------------- | ----------------- | ------------------------------------------------------------ |
| [无锡暖芯半导体科技有限公司 (tasc-nx.com)                    | [TD2000D-G](http://www.tasc-nx.com/pd.jsp?fromColId=2&id=37#_pp=2_440) | RS-485 2Wire | 9600-8-none-1 | N/A  | ModbusRTU                             | 3.5字节的时间间隔 | CRC16                                                        |
| [Vacuum Abatement Systems & Cleanroom Equipment – EBARA Technologies, Inc.](https://www.ebaratech.com/) | [Model EV-S20P](https://www.ebaratech.com/products/dry-vacuum-pumps/light-duty-model-ev-s/model-ev-s20/) | RS-232       | 9600-8-none-1 | N/A  | STX-ascii format payload-ETX-Check-CR | CR终止符          | 2个字节：累加取和的最后一个字节，如0x9A,然后校验码是9和A对应的Ascii编码数字。 |
|                                                              |                                                              |              |               |      |                                       |                   |                                                              |
