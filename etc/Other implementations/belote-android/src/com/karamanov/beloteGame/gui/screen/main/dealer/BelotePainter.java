/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.screen.main.dealer;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Paint.Style;
import android.graphics.Rect;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.NinePatchDrawable;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceList;
import belote.bean.announce.AnnounceUnit;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.announce.type.AnnounceType;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.logic.BeloteFacade;
import belote.logic.HumanBeloteFacade;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.gui.screen.base.BasePainter;
import com.karamanov.beloteGame.gui.screen.main.BeloteView;
import com.karamanov.beloteGame.text.PlayerNameDecorator;
import com.karamanov.beloteGame.text.ShortPlayerNameDecorator;
import com.karamanov.framework.graphics.Color;
import com.karamanov.framework.graphics.ImageUtil;
import com.karamanov.framework.graphics.Rectangle;

/**
 * BelotPainter class.
 * @author Dimitar Karamanov
 */
final class BelotePainter extends BasePainter {

    /**
     * Player with ID(0) cards overlapped
     */
    private final int Player0CardsOverlapped;

    /**
     * Player with ID(2) cards overlapped constant.
     */
    private int Player2CardsOverlapped;

    /**
     * Players with ID(1,3) cards overlapped (vertical).
     */
    private final int Player1Player3CardsOverlapped;

    /**
     * Game font height.
     */
    private final int GAME_FONT_HEIGHT;

    /**
     * Constructor.
     * @param width canvas width.
     * @param height canvas height.
     */
    public BelotePainter(Context context) {
        super(context);

        Player0CardsOverlapped = Belote.fromPixelToDip(context, 18);
        Player2CardsOverlapped = Belote.fromPixelToDip(context, -7);
        Player1Player3CardsOverlapped = Belote.fromPixelToDip(context, 21);

        GAME_FONT_HEIGHT = Belote.fromPixelToDip(context, 14);
    }

    public int getFontHeight() {
        return GAME_FONT_HEIGHT;
    }

    /**
     * Draws player name.
     * @param game BelotGame instance.
     * @param g Graphics instance.
     * @param player which name is vertical drawn.
     */
    private void drawPlayerName(Canvas canvas, HumanBeloteFacade game, Player player) {
        int x;
        int y;

        Rectangle rec = getPlayerCardRectangle(canvas, game, 0, player);

        Paint paint = new Paint();
        float dip12 = Belote.fromPixelToDipF(context, 12);
        int dip2 = Belote.fromPixelToDip(context, 2);
        paint.setTextSize(dip12);
        paint.setColor(Color.clCream.getRGB());

        boolean active = player.equals(game.getGame().getTrickAttackPlayer());

        switch (player.getID()) {
        case 0:
            Rect bounds = new Rect();
            PlayerNameDecorator playerDecorator = new PlayerNameDecorator(player);
            String name = playerDecorator.decorate(context).toUpperCase();
            paint.getTextBounds(name, 0, name.length(), bounds);
            y = rec.y - dip2;
            drawHorizontalPlayerName(canvas, paint, player, y, active);
            return;
        case 1:
            x = 1;
            drawVerticalPlayerName(canvas, paint, player, x, active);
            return;
        case 2:
            bounds = new Rect();
            playerDecorator = new PlayerNameDecorator(player);
            name = playerDecorator.decorate(context).toUpperCase();
            paint.getTextBounds(name, 0, name.length(), bounds);

            y = rec.height + rec.y + bounds.height() + dip2;
            drawHorizontalPlayerName(canvas, paint, player, y, active);

            return;
        case 3:
            x = rec.x + rec.width;
            drawVerticalPlayerName(canvas, paint, player, x, active);
            return;
        }
    }

    /**
     * Draws horizontal player name.
     * @param g Graphics instance.
     * @param player which name is vertical drawn.
     * @param y position.
     * @param active status.
     */
    private void drawHorizontalPlayerName(Canvas canvas, Paint paint, Player player, int y, boolean active) {
        Rect bounds = new Rect();
        PlayerNameDecorator playerDecorator = new PlayerNameDecorator(player);
        String name = playerDecorator.decorate(context).toUpperCase();
        paint.getTextBounds(name, 0, name.length(), bounds);
        final int x = (canvas.getWidth() - bounds.width()) / 2;
        paint.setColor(active ? Color.clDKGold.getRGB() : Color.clLightYellow.getRGB());
        paint.setFakeBoldText(true);
        paint.setAntiAlias(true);
        paint.setDither(true);
        paint.setLinearText(true);
        canvas.drawText(name, x, y, paint);
    }

    /**
     * Draws vertical player name.
     * @param g Graphics instance.
     * @param player which name is vertical drawn.
     * @param x position.
     * @param active status.
     */
    private void drawVerticalPlayerName(Canvas canvas, Paint paint, Player player, int x, boolean active) {
        int theta = 0;

        if (player.getID() == 1) {
            theta = 90;
        }

        if (player.getID() == 3) {
            theta = -90;
        }

        Rect bounds = new Rect();

        PlayerNameDecorator playerDecorator = new PlayerNameDecorator(player);
        String name = playerDecorator.decorate(context).toUpperCase();

        paint.getTextBounds(name, 0, name.length(), bounds);
        int y = (canvas.getHeight() - bounds.width()) / 2;
        paint.setColor(active ? Color.clDKGold.getRGB() : Color.clLightYellow.getRGB());

        paint.setFakeBoldText(true);
        paint.setAntiAlias(true);
        paint.setDither(true);
        paint.setLinearText(true);

        canvas.save();
        try {
            canvas.rotate(theta);
            if (player.getID() == 1) {
                canvas.drawText(name.toUpperCase(), y, -x, paint);
            } else {
                canvas.drawText(name.toUpperCase(), -y - bounds.width(), canvas.getWidth() - 1, paint);
            }
        } finally {
            canvas.restore();
        }
    }

    /**
     * Returns players card rectangle.
     * @param game BelotGame instance.
     * @param index of card.
     * @param player which card rectangle is retrieved.
     * @return Rectangle player' card one.
     */
    public final Rectangle getPlayerCardRectangle(Canvas canvas, HumanBeloteFacade game, final int index, Player player) {
        int x = 0;
        int y = 0;

        switch (player.getID()) {
        case 0:
            x = getPlayer0FirstCardX(canvas, game) + index * (cardBackWidth - Player0CardsOverlapped);
            y = GAME_FONT_HEIGHT;
            break;

        case 1:
            x = GAME_FONT_HEIGHT;
            y = getFirstCardPosY(canvas, game) + index * (cardBackWidth - Player1Player3CardsOverlapped);
            break;

        case 2:
            x = getPlayer2FirstCardX(canvas, game) + index * (cardWidth - Player2CardsOverlapped);
            y = canvas.getHeight() - GAME_FONT_HEIGHT - cardHeight;
            if (index == player.getCards().getSize() - 1) {
                return new Rectangle(x, y, cardWidth, cardHeight);
            } else {
                return new Rectangle(x, y, cardWidth - Player2CardsOverlapped, cardHeight);
            }

        case 3:
            x = canvas.getWidth() - GAME_FONT_HEIGHT - cardBackHeight;
            y = getFirstCardPosY(canvas, game) + index * (cardBackWidth - Player1Player3CardsOverlapped);
            break;
        }

        if (player.isSameTeam(game.getHumanPlayer())) {
            if (player.getID() == 2) {
                return new Rectangle(x, y, cardWidth, cardHeight);
            } else {
                return new Rectangle(x, y, cardBackWidth, cardBackHeight);
            }
        } else {
            return new Rectangle(x, y, cardBackHeight, cardBackWidth);
        }
    }

    private int getCardsCount(BeloteFacade game) {
        final int nCards = (game.isAnnounceGameMode()) ? 5 : 8;
        return nCards;
    }

    /**
     * Returns first card X position.
     * @param game BelotGame instance.
     * @return int position.
     */
    private int getPlayer2FirstCardX(Canvas canvas, BeloteFacade game) {
        final int nCards = getCardsCount(game);
        int dip2 = Belote.fromPixelToDip(context, 2);

        int avaiableWidth = canvas.getWidth() - dip2 - 2 * GAME_FONT_HEIGHT;
        if (canvas.getWidth() > canvas.getHeight()) {
            avaiableWidth = avaiableWidth - dip2 - 2 * cardBackHeight; // Think
                                                                       // more
                                                                       // !
        }

        if (nCards * cardWidth > (avaiableWidth)) {
            double d = (double) (nCards * cardWidth - avaiableWidth) / (nCards - 1);
            Player2CardsOverlapped = (int) Math.ceil(d);
        } else {
            Player2CardsOverlapped = 0;
        }

        return (canvas.getWidth() - nCards * cardWidth + (nCards - 1) * Player2CardsOverlapped) / 2 + 1;
    }

    /**
     * Returns first card XX position.
     * @param game BelotGame instance.
     * @return position.
     */
    private int getPlayer0FirstCardX(Canvas canvas, BeloteFacade game) {
        final int nCards = getCardsCount(game);
        return (canvas.getWidth() - nCards * cardBackWidth + (nCards - 1) * Player0CardsOverlapped) / 2;
    }

    /**
     * Returns first card y position.
     * @param game BelotGame instance.
     * @return position.
     */
    private int getFirstCardPosY(Canvas canvas, BeloteFacade game) {
        final int nCards = getCardsCount(game);
        return (canvas.getHeight() - nCards * cardBackWidth + (nCards - 1) * Player1Player3CardsOverlapped) / 2;
    }

    /**
     * Returns announce text.
     * @param announce which text is retrieves.
     * @return String announce text.
     */
    private String getAnnounceText(Announce announce) {
        String result;
        if (announce.isTrumpAnnounce()) {

            if (announce.getType().equals(AnnounceType.Double)) {
                result = textDecorator.getAnnounceType(AnnounceType.Double);
            } else if (announce.getType().equals(AnnounceType.Redouble)) {
                result = textDecorator.getAnnounceType(AnnounceType.Redouble);
            } else {
                result = textDecorator.getAnnounceSuit(announce.getAnnounceSuit());
            }
        } else {
            if (announce.getType().equals(AnnounceType.Double)) {
                result = textDecorator.getAnnounceSuitShort(announce.getAnnounceSuit()) + " " + textDecorator.getAnnounceType(AnnounceType.Double);
            } else if (announce.getType().equals(AnnounceType.Redouble)) {
                result = textDecorator.getAnnounceSuitShort(announce.getAnnounceSuit()) + " " + textDecorator.getAnnounceType(AnnounceType.Redouble);
            } else {
                result = textDecorator.getAnnounceSuit(announce.getAnnounceSuit());
            }
        }

        return result;
    }

    /**
     * Returns announce rectangle.
     * @param game BelotGame instance.
     * @param announce to be draw.
     * @param g Graphics instance.
     * @return Rectangle of the announce.
     */
    private Rectangle getAnnounceRectangle(Canvas canvas, HumanBeloteFacade game, Announce announce, Paint paint) {
        int dip2 = Belote.fromPixelToDip(context, 2);
        int dip4 = Belote.fromPixelToDip(context, 4);
        int dip6 = Belote.fromPixelToDip(context, 6);
        int dip10 = Belote.fromPixelToDip(context, 10);

        Rectangle rect = getPlayerCardRectangle(canvas, game, 0, announce.getPlayer());
        int x = 0;
        int y = 0;

        int w;
        int h;
        String str = getAnnounceText(announce);

        Rect bounds = new Rect();
        paint.getTextBounds(str, 0, str.length(), bounds);

        if (announce.isTrumpAnnounce()) {
            Suit suit = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
            Bitmap image = getSuitImage(suit);
            w = bounds.width() + dip6 + image.getWidth() + dip4;
            h = Math.max(image.getHeight(), bounds.height());
        } else {
            w = bounds.width() + dip6;
            h = bounds.height();
        }

        switch (announce.getPlayer().getID()) {
        case 0:
            x = (canvas.getWidth() - w) / 2;
            y = rect.y + rect.height + dip10;
            break;

        case 1:
            x = rect.x + rect.width + dip10;
            y = (canvas.getHeight() - h) / 2;
            break;

        case 2:
            x = (canvas.getWidth() - w) / 2;
            y = rect.y - dip10 - h;
            break;

        case 3:
            y = (canvas.getHeight() - h) / 2;
            x = rect.x - dip10 - w;
            break;
        }

        return new Rectangle(x - dip2, y, w + dip4, h);
    }

    /**
     * Draws last announce.
     * @param game BelotGame instance.
     * @param g Graphics instance.
     */
    private Rect drawLastAnnounce(Canvas canvas, HumanBeloteFacade game) {
        if (game.isAnnounceGameMode()) {
            final int count = game.getGame().getAnnounceList().getCount();
            if (count > 0) {
                if (!game.isPlayerIsAnnoincing()) {
                    return drawAnnounce(canvas, game, game.getGame().getAnnounceList().getAnnounce(count - 1));
                }
            }
        }
        return null;
    }

    /**
     * Draws announce.
     * @param game BelotGame instance.
     * @param announce to be draw.
     * @param g Graphics instance.
     */
    private Rect drawAnnounce(Canvas canvas, HumanBeloteFacade game, Announce announce) {
        Paint paint = new Paint();
        paint.setColor(Color.clDarkRed.getRGB());
        paint.setFakeBoldText(true);
        paint.setAntiAlias(true);
        paint.setDither(true);
        paint.setLinearText(true);

        float dip14 = Belote.fromPixelToDipF(context, 14);
        paint.setTextSize(dip14);
        int dip2 = Belote.fromPixelToDip(context, 2);
        int dip10 = Belote.fromPixelToDip(context, 10);

        NinePatchDrawable bubble = announce.getPlayer().getTeam().equals(game.getGame().getTeam(0)) ? pictureDecorator.getBubbleLeft() : pictureDecorator
                .getBubbleRight();

        String str = getAnnounceText(announce);
        Rect bounds = new Rect();
        paint.getTextBounds(str, 0, str.length(), bounds);

        Rect dest = getBubbleAnnounceRectangle(canvas, game, announce);
        bubble.setBounds(dest);
        bubble.draw(canvas);

        if (announce.isTrumpAnnounce()) {
            Suit suit = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
            Bitmap image = getSuitImage(suit);
            canvas.drawBitmap(image, dest.left + dip10, 2 * dip10 + dest.top - bounds.height() + (bounds.height() - image.getHeight()) / 2, paint);
            canvas.drawText(str, dest.left + dip10 + dip2 + image.getWidth(), 2 * dip10 + dest.top, paint);
        } else {
            canvas.drawText(str, dest.left + dip10, 2 * dip10 + dest.top, paint);
        }

        return dest;
    }

    private Rect getBubbleAnnounceRectangle(Canvas canvas, HumanBeloteFacade game, Announce announce) {
        Paint paint = new Paint();
        paint.setFakeBoldText(true);
        paint.setAntiAlias(true);
        paint.setDither(true);
        paint.setLinearText(true);

        float dip14 = Belote.fromPixelToDipF(context, 14);
        paint.setTextSize(dip14);

        paint.setColor(Color.clDarkGreen.getRGB());
        paint.setStyle(Style.FILL);

        String str = getAnnounceText(announce);
        Rect bounds = new Rect();
        paint.getTextBounds(str, 0, str.length(), bounds);

        Rect dest = new Rect();
        Rectangle rect = getAnnounceRectangle(canvas, game, announce, paint);

        int dip1 = Belote.fromPixelToDip(context, 1);
        int dip2 = Belote.fromPixelToDip(context, 2);
        int dip3 = Belote.fromPixelToDip(context, 3);
        int dip10 = Belote.fromPixelToDip(context, 10);

        if (announce.isTrumpAnnounce()) {
            int x = rect.x + dip3;
            Suit suit = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
            Bitmap image = getSuitImage(suit);
            int y = rect.y;

            if (announce.getPlayer().getID() == 0) {
                y += dip1;
            }

            if (announce.getPlayer().getID() == 2) {
                y -= dip1;
            }

            dest.left = x - dip10;
            dest.top = y - dip10;
            dest.bottom = dest.top + Math.max(image.getHeight(), bounds.height()) + dip10 * 2;
            dest.right = dest.left + image.getWidth() + dip2 + bounds.width() + dip10 * 2;
        } else {
            int x = rect.x + dip3;
            int y = rect.y;

            if (announce.getPlayer().getID() == 0) {
                y += dip1;
            }

            if (announce.getPlayer().getID() == 2) {
                y -= dip1;
            }

            dest.left = x - dip10;
            dest.top = y - dip10;
            dest.bottom = dest.top + bounds.height() + 2 * dip10;
            dest.right = dest.left + bounds.width() + 2 * dip10;
        }

        return dest;
    }

    /**
     * Draws score.
     * @param game BelotGame instance.
     * @param g Graphics instance.
     */
    private void drawAnnounce(Canvas canvas, BeloteFacade game) {
        if (game.isPlayingGameMode()) {
            Paint paint = new Paint();
            float dip12 = Belote.fromPixelToDipF(context, 12);
            paint.setTextSize(dip12);

            paint.setFakeBoldText(true);
            paint.setAntiAlias(true);

            Rect bounds = new Rect();
            paint.getTextBounds("|", 0, 1, bounds);
            final int maxY = bounds.height();
            int y = maxY;
            int dip5 = Belote.fromPixelToDip(context, 5);
            int dip1 = Belote.fromPixelToDip(context, 1);

            Announce announce = game.getGame().getAnnounceList().getOpenContractAnnounce();

            ShortPlayerNameDecorator decorator = new ShortPlayerNameDecorator(announce.getPlayer());
            String playerShort = decorator.decorate(context);

            bounds = new Rect();
            paint.getTextBounds(playerShort, 0, playerShort.length(), bounds);
            int x = canvas.getWidth() - bounds.width() - dip1;
            paint.setColor(Color.clCream.getRGB());
            canvas.drawText(playerShort, x, y, paint);

            if (announce.getAnnounceSuit().equals(AnnounceSuit.AllTrump) || announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump)) {
                String announceShort = announce.getAnnounceSuit().equals(AnnounceSuit.AllTrump) ? context.getString(R.string.AllTrumpsAnnounceShort) : context
                        .getString(R.string.NotTrumpsAnnounceShort);

                bounds = new Rect();
                paint.getTextBounds(announceShort, 0, announceShort.length(), bounds);
                x -= bounds.width() + dip5;
                paint.setColor(Color.clLightGreen.getRGB());
                canvas.drawText(announceShort, x, y, paint);
            } else {
                if (announce.isTrumpAnnounce()) {
                    Suit suit = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
                    Bitmap image = getSuitImage(suit);
                    image = ImageUtil.transformToMixedColorImage(image, new Color(192), null);
                    x -= image.getWidth() + dip5;
                    y = dip1;
                    if (bounds.height() - image.getHeight() > 0) {
                        y = (bounds.height() - image.getHeight()) / 2;
                    }
                    canvas.drawBitmap(image, x, y, paint);

                    y = maxY;
                }
            }

            AnnounceList announces = game.getGame().getAnnounceList().getSuitAnnounces(announce.getAnnounceSuit());
            Announce dbl = announces.getDoubleAnnounce();
            Announce redbl = announces.getRedoubleAnnounce();

            int maxNameWidth = 0;
            int maxAnnounceWidth = 0;

            if (dbl != null) {
                decorator = new ShortPlayerNameDecorator(dbl.getPlayer());
                playerShort = decorator.decorate(context);

                bounds = new Rect();
                paint.getTextBounds(playerShort, 0, playerShort.length(), bounds);

                maxNameWidth = Math.max(maxNameWidth, bounds.width());

                String announceShort = context.getString(R.string.DoubleAnnounce);
                bounds = new Rect();
                paint.getTextBounds(announceShort, 0, 1, bounds);

                maxAnnounceWidth = Math.max(maxAnnounceWidth, bounds.width());
            }

            if (redbl != null) {
                decorator = new ShortPlayerNameDecorator(redbl.getPlayer());
                playerShort = decorator.decorate(context);

                bounds = new Rect();
                paint.getTextBounds(playerShort, 0, playerShort.length(), bounds);

                maxNameWidth = Math.max(maxNameWidth, bounds.width());

                String announceShort = context.getString(R.string.RedoubleAnnounce);
                bounds = new Rect();
                paint.getTextBounds(announceShort, 0, 1, bounds);

                maxAnnounceWidth = Math.max(maxAnnounceWidth, bounds.width());
            }

            if (dbl != null) {
                y += maxY;

                decorator = new ShortPlayerNameDecorator(dbl.getPlayer());
                playerShort = decorator.decorate(context);

                x = canvas.getWidth() - maxNameWidth - dip1;
                paint.setColor(Color.clCream.getRGB());
                canvas.drawText(playerShort, x, y, paint);

                String announceShort = context.getString(R.string.DoubleAnnounce);
                x -= maxAnnounceWidth + dip5;
                paint.setColor(Color.clRed.getRGB());
                canvas.drawText(String.valueOf(announceShort.charAt(0)), x, y, paint);
            }

            if (redbl != null) {
                y += maxY;

                decorator = new ShortPlayerNameDecorator(redbl.getPlayer());
                playerShort = decorator.decorate(context);

                x = canvas.getWidth() - maxNameWidth - dip1;
                paint.setColor(Color.clCream.getRGB());
                canvas.drawText(playerShort, x, y, paint);

                String announceShort = context.getString(R.string.RedoubleAnnounce);
                x -= maxAnnounceWidth + dip5;
                paint.setColor(Color.clDKGold.getRGB());
                canvas.drawText(String.valueOf(announceShort.charAt(0)), x, y, paint);
            }
        }
    }

    /**
     * Draws score.
     * @param game BelotGame instance.
     * @param g Graphics instance.
     */
    private void drawScore(Canvas canvas, BeloteFacade game) {
        float dip12 = Belote.fromPixelToDipF(context, 12);
        Paint paint = new Paint();
        paint.setTextSize(dip12);
        paint.setColor(Color.clLightGreen.getRGB());
        paint.setFakeBoldText(true);
        paint.setAntiAlias(true);
        paint.setDither(true);

        int maxWidth = 0;
        int maxHeight = 0;
        
        Rect boundsDigids = new Rect();
        String maxScore = " 99999";
        paint.getTextBounds(maxScore, 0, maxScore.length(), boundsDigids);
        
        for (int i = 0; i < game.getGame().getTeamsCount(); i++) {
            StringBuffer team = new StringBuffer();
            for (int j = 0; j < game.getGame().getTeam(i).getPlayersCount(); j++) {
                if (team.length() > 0) {
                    team.append("-");
                }

                team.append(new ShortPlayerNameDecorator(game.getGame().getTeam(i).getPlayer(j)).decorate(context));
            }

            String score = team.toString();
            Rect bounds = new Rect();
            paint.getTextBounds(score + "|", 0, score.length() + 1, bounds);
            
            maxHeight = Math.max(maxHeight, bounds.height());
            maxWidth = Math.max(maxWidth, bounds.width());
            
            canvas.drawText(score, i * (boundsDigids.width() + maxWidth), bounds.height(), paint);
        }

        paint.setColor(Color.clCream.getRGB());
        for (int i = 0; i < game.getGame().getTeamsCount(); i++) {
            String score = " " + game.getGame().getTeam(i).getPoints().getAllPoints();
            canvas.drawText(score, maxWidth + i * (maxWidth + boundsDigids.width()), maxHeight, paint);
        }
    }

    /**
     * Draws played hand cards.
     * @param game BelotGame instance.
     * @param g Graphics instance.
     */
    private void drawCurrentTrickCards(Canvas canvas, HumanBeloteFacade game) {
        Player player = game.getGame().getTrickAttackPlayer();
        for (PackIterator iterator = game.getGame().getTrickCards().iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            drawPlayedCardByPlayer(canvas, game, player, card);
            player = game.getPlayerAfter(player);
        }
    }

    /**
     * Draws player hand.
     * @param game BelotGame instance.
     * @param player which rectangle is retrieved.
     * @param card played card.
     * @param g Graphics instance.
     */
    private void drawPlayedCardByPlayer(Canvas canvas, HumanBeloteFacade game, Player player, Card card) {
        final Rectangle rec = getPlayerPlayedCardRectangle(canvas, game, player);

        final Player winner = game.getNextTrickAttackPlayer();
        if (player.equals(winner)) {
            drawMixedColorCard(canvas, card, rec.x, rec.y, Color.clPureYellow);
        } else {
            drawCard(canvas, card, rec.x, rec.y);
        }
    }

    /**
     * Returns player' played card rectangle.
     * @param game BelotGame instance.
     * @param player which rectangle is retrieved.
     * @return Rectangle instance.
     */
    private Rectangle getPlayerPlayedCardRectangle(Canvas canvas, HumanBeloteFacade game, Player player) {
        Rectangle rect = getPlayerCardRectangle(canvas, game, 0, player);
        int x = 0;
        int y = 0;
        int dip4 = Belote.fromPixelToDip(context, 4);

        switch (player.getID()) {
        case 0:
            x = (canvas.getWidth() - cardWidth) / 2;
            y = rect.y + rect.height + dip4;
            break;

        case 1:
            x = rect.x + dip4 + rect.width;
            y = (canvas.getHeight() - cardHeight) / 2;
            break;

        case 2:
            x = (canvas.getWidth() - cardWidth) / 2;
            y = rect.y - dip4 - cardHeight;
            break;

        case 3:
            y = (canvas.getHeight() - cardHeight) / 2;
            x = rect.x - dip4 - cardWidth;
            break;
        }

        return new Rectangle(x, y, cardWidth, cardHeight);
    }

    /**
     * Draws players cards. Moved here the method because uses Canvas methods - flushGraphics() which is preffered to be used only from this class.
     * @param g Graphics object.
     * @param delay between drawing cards.
     */
    private void drawPlayersNames(Canvas canvas, BeloteView view, HumanBeloteFacade game) {
        for (int i = 0; i < game.getGame().getPlayersCount(); i++) {
            drawPlayerName(canvas, game, game.getGame().getPlayer(i));
        }
    }

    /**
     * Draws players cards. Moved here the method because uses Canvas methods - flushGraphics() which is preffered to be used only from this class.
     * @param g Graphics object.
     * @param delay between drawing cards.
     */
    private void drawPlayersCards(Canvas canvas, BeloteView view, HumanBeloteFacade game, long delay) {
        Player player = game.getGame().getDealAttackPlayer();
        for (int i = 0; i < game.getGame().getPlayersCount(); i++) {
            drawPlayerCards(canvas, view, game, player, delay);
            player = game.getPlayerAfter(player);
        }
    }

    /**
     * Draws player cards. Moved here the method because uses Canvas methods - flushGraphics() which is preffered to be used only from this class.
     * @param canvas Graphics object.
     * @param player player which cards are drawn.
     * @param delay between drawing cards.
     */
    private void drawPlayerCards(Canvas canvas, final BeloteView view, HumanBeloteFacade game, Player player, long delay) {
        int nCards = (game.isAnnounceGameMode()) ? BeloteFacade.ANNOUNCE_CARD_COUNT : Rank.getRankCount();
        for (int i = 0; i < nCards; i++) {

            if (i < player.getCards().getSize()) {
                Rectangle rec = getPlayerCardRectangle(canvas, game, i, player);
                Card card = player.getCards().getCard(i);

                if (player.equals(game.getHumanPlayer())) {
                    if (card.equals(game.getHumanPlayer().getSelectedCard())) {
                        drawDarkenedCard(canvas, card, rec.x, rec.y);
                    } else {
                        drawCard(canvas, card, rec.x, rec.y);
                    }
                } else {
                    if (player.isSameTeam(game.getHumanPlayer())) {
                        drawCardBackImage(canvas, rec.x, rec.y);
                    } else {
                        drawRotatedCardBackImage(canvas, rec.x + rec.width, rec.y);
                    }
                }

                if (delay > 0) {
                    view.refresh();
                    sleep(delay);
                }
            }
        }
    }

    /**
     * Sleeps for provided millisecond.
     * @param ms provided millisecond.
     */
    private void sleep(final long ms) {
        if (ms > 0) {
            try {
                Thread.sleep(ms);
            } catch (InterruptedException ex) {
                // D.N.
            }
        }
    }

    private void clearBackground(Canvas graphics) {
        GradientDrawable bkg = pictureDecorator.getMainBKG();
        bkg.setDither(true);
        bkg.setBounds(new Rect(0, 0, graphics.getWidth(), graphics.getHeight()));
        bkg.draw(graphics);
    }

    public final void drawGame(Canvas graphics, HumanBeloteFacade game, BeloteView view, long delay) {
        clearBackground(graphics);
        drawScore(graphics, game);
        drawAnnounce(graphics, game);
        drawPlayersNames(graphics, view, game);
        drawPlayersCards(graphics, view, game, delay);
        drawCurrentTrickCards(graphics, game);
        drawLastAnnounce(graphics, game);
    }
}
