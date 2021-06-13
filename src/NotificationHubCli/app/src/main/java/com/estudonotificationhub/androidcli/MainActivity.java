package com.estudonotificationhub.androidcli;

import androidx.appcompat.app.AppCompatActivity;

import android.os.Bundle;
import android.widget.TextView;
import android.widget.Toast;

import com.microsoft.windowsazure.messaging.notificationhubs.NotificationHub;

public class MainActivity extends AppCompatActivity {
    public static MainActivity mainActivity;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        mainActivity = this;
        NotificationHub.setListener(new CustomNotificationListener());
        NotificationHub.start(this.getApplication(),
                "comgas-dev-hub",
                "Endpoint=sb://comgas-dev-nh.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=UdWZFKzTvwtKwgVVLlbSayVCNMl9GJDHPQDoSaZXxuo=");
    }

    public void ToastNotify(final String notificationMessage) {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Toast.makeText(MainActivity.this, notificationMessage, Toast.LENGTH_LONG).show();
                TextView helloText = (TextView) findViewById(R.id.text_hello);
                helloText.setText(notificationMessage);
            }
        });
    }
}