package com.karamanov.beloteGame.gui.screen.tricks;

import android.content.Context;
import android.graphics.Bitmap;
import android.widget.ImageView;
import android.widget.LinearLayout;
import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.bean.trick.Trick;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.gui.graphics.PictureDecorator;
import com.karamanov.framework.graphics.Color;
import com.karamanov.framework.graphics.ImageUtil;
import com.karamanov.framework.graphics.Rectangle;

public final class TrickView extends LinearLayout {

    public TrickView(Context context, Trick trick, Game game) {
        super(context);

        int dip2 = Belote.fromPixelToDip(context, 2);
        setOrientation(LinearLayout.HORIZONTAL);

        PictureDecorator decorator = new PictureDecorator(context);

        Player player = trick.getAttackPlayer();
        for (PackIterator packIterator = trick.iterator(); packIterator.hasNext(); player = game.getPlayerAfter(player)) {
            ImageView imageView = new ImageView(context);
            Card card = packIterator.next();

            if (player.equals(trick.getWinnerPlayer())) {
                Bitmap picture = decorator.getCardImage(card);
                if (picture != null) {
                    final Rectangle rec = new Rectangle(0, 0, picture.getWidth(), picture.getHeight());
                    Bitmap bitmap = ImageUtil.transformToMixedColorImage(picture, Color.clPureYellow, rec);
                    imageView.setImageBitmap(bitmap);
                }
            } else {
                imageView.setImageBitmap(decorator.getCardImage(card));
            }

            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
            params.setMargins(dip2, dip2, dip2, dip2);
            imageView.setLayoutParams(params);
            addView(imageView);
        }
    }
}
