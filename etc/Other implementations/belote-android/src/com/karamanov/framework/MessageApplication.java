package com.karamanov.framework;

import android.app.Application;
import android.content.Context;
import android.content.res.Resources;
import android.util.TypedValue;

import com.karamanov.framework.message.MessageProcessor;
import com.karamanov.framework.message.MessageType;

public class MessageApplication extends Application {
    
    public final static MessageType MT_KEY_PRESSED = new MessageType("MT_KEY_PRESSED");

    public final static MessageType MT_TOUCH_EVENT = new MessageType("MT_TOUCH_EVENT");

    public final static MessageType MT_EXIT_EVENT = new MessageType("MT_EXIT_EVENT");

    public final static MessageType MT_PAINT_EVENT = new MessageType("MT_PAINT_EVENT");
    
    public final static MessageType MT_CLOSE_END_GAME = new MessageType("MT_CLOSE_END_GAME");

    private final MessageProcessor messageProcessor;

    public MessageApplication() {
        super();
        messageProcessor = new MessageProcessor();
    }

    @Override
    public void onCreate() {
        super.onCreate();
        messageProcessor.start();
    }

    public final MessageProcessor getMessageProcessor() {
        return messageProcessor;
    }

    public static int fromPixelToDip(Context context, int pixels) {
        Resources resources = context.getResources();
        if (pixels == 1) {
            return Math.max(1, Math.round(TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, pixels, resources.getDisplayMetrics())));
        } else {
            return Math.round(TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, pixels, resources.getDisplayMetrics()));
        }
    }

    public static float fromPixelToDipF(Context context, float pixels) {
        Resources resources = context.getResources();
        return TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, pixels, resources.getDisplayMetrics());
    }
}
