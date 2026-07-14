using App.Network.Tcp;

namespace App.Tests;

public class TcpConnectionTests
{
    [Fact]
    public void ReceiveData_AccumulatesBytesUntilConsumed()
    {
        var connection = new TcpConnection();

        connection.ReceiveData([0x47, 0x45]);
        connection.ReceiveData([0x54, 0x20]);
        connection.ConsumeData(3);

        Assert.Equal(new byte[] { 0x20 }, connection.GetReceivedData());
    }

    [Fact]
    public void GetReceivedData_ReturnsCopyOfBufferedData()
    {
        var connection = new TcpConnection();
        connection.ReceiveData([0x47]);

        byte[] receivedData = connection.GetReceivedData();
        receivedData[0] = 0x58;

        Assert.Equal(new byte[] { 0x47 }, connection.GetReceivedData());
    }

    [Fact]
    public void ConsumeData_MoreThanBufferedBytes_ThrowsArgumentException()
    {
        var connection = new TcpConnection();
        connection.ReceiveData([0x47]);

        Assert.Throws<ArgumentException>(() => connection.ConsumeData(2));
    }
}