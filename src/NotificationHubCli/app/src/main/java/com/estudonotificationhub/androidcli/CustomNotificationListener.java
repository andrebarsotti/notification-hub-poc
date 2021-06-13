package com.estudonotificationhub.androidcli;

import android.content.Context;
import android.util.Log;

import com.google.firebase.messaging.RemoteMessage;
import com.microsoft.windowsazure.messaging.notificationhubs.NotificationListener;

import java.util.Map;

import static android.content.ContentValues.TAG;

public class CustomNotificationListener implements NotificationListener {

    @Override
    public void onPushNotificationReceived(Context context, RemoteMessage message) {
        /* The following notification properties are available. */
        RemoteMessage.Notification notification = message.getNotification();
        String title = notification.getTitle();
        String body = notification.getBody();
        Map<String, String> data = message.getData();

        if (message != null) {
            Log.d(TAG, "Message Notification Title: " + title);
            Log.d(TAG, "Message Notification Body: " + body);
            MainActivity.mainActivity.ToastNotify(title);
        }

        if (data != null) {
            for (Map.Entry<String, String> entry : data.entrySet()) {
                Log.d(TAG, "key, " + entry.getKey() + " value " + entry.getValue());
            }
        }
    }
}
