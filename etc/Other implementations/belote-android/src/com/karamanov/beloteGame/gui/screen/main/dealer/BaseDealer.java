package com.karamanov.beloteGame.gui.screen.main.dealer;

import java.util.ArrayList;

import android.graphics.Canvas;
import android.os.Handler;
import android.view.Gravity;
import android.view.View;
import android.view.WindowManager;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.pack.card.Card;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceType;
import belote.bean.pack.square.SquareIterator;
import belote.logic.HumanBeloteFacade;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.gui.screen.main.BeloteView;
import com.karamanov.beloteGame.gui.screen.main.message.MessageData;
import com.karamanov.beloteGame.gui.screen.main.message.MessageScreen;
import com.karamanov.framework.BooleanFlag;
import com.karamanov.framework.MessageActivity;
import com.karamanov.framework.graphics.Rectangle;

/**
 * BaseDealer class.
 * @author Dimitar Karamanov
 */
abstract class BaseDealer {
    
    /**
     * Handler to GUI thread.
     */
    protected final Handler handler;

    /**
     * Standard card delay on painting (effect).
     */
    public static final int STANDARD_CARD_DELAY = 20;

    /**
     * Belote painter. (All drawing functionality is in it).
     */
    public final BelotePainter belotPainter;

    /**
     * Delay constant
     */
    protected final static int PLAY_DELAY = 200;

    protected final MessageActivity context;

    protected final BeloteView belotPanel;

    protected final View buttons;
    
    protected final HumanBeloteFacade beloteFacade;

    private MessageScreen messageScreen;
    
    protected BaseDealer(MessageActivity context, BeloteView belotPanel, View buttons) {
        this.context = context;
        this.belotPanel = belotPanel;
        this.buttons = buttons;
        this.beloteFacade = Belote.getBeloteFacade(context);

        belotPainter = new BelotePainter(context);
        handler = new Handler();
    }

    /**
     * Checks key click.
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    public abstract void checkKeyPressed(int keyCode);
    
    /**
     * Checks pointer click.
     * @param x position.
     * @param y position.
     */
    public abstract void checkPointerPressed(float x, float y);
    
    /**
     * Invalidate game on belote panel.
     */
    public final void invalidateGame() {
        invalidateGame(0);
    }

    /**
     * Invalidate game on belote panel.
     */
    public final void invalidateGame(int delay) {
        Canvas canvas = belotPanel.getBufferedCanvas();
        if (canvas != null) {
            belotPainter.drawGame(canvas, beloteFacade, belotPanel, delay);
            belotPanel.refresh();
        }
    }
    
    /**
     * Creates a message list for provided player.
     * @param player
     * @param card
     * @return
     */
    protected final ArrayList<MessageData> getMessageList(final Player player, final Card card) {
        ArrayList<MessageData> result = new ArrayList<MessageData>();

        if (player.equals(beloteFacade.getGame().getTrickCouplePlayer())) {
            result.add(new MessageData(belotPainter.getCoupleImage(card.getSuit()), context.getString(R.string.Belote)));
        }
        // Add equals and sequences messages on first round and when the game is
        // not NT.
        Announce announce = beloteFacade.getGame().getAnnounceList().getOpenContractAnnounce();
        if (announce != null && !announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump) && beloteFacade.getGame().getTrickList().isEmpty()) {
            for (SquareIterator it = player.getCards().getSquaresList().iterator(); it.hasNext();) {
                result.add(new MessageData(belotPainter.getEqualCardsImage(it.next()), context.getString(R.string.FourCards)));
            }

            for (SequenceIterator it = player.getCards().getSequencesList().iterator(); it.hasNext();) {
                Sequence sequence = it.next();
                result.add(new MessageData(belotPainter.getSequenceTypeImage(sequence.getType()), getSequenceString(sequence.getType())));
            }
        }

        return result;
    }
    
    /**
     * Gets sequence text by type.
     * @param type
     * @return
     */
    private String getSequenceString(SequenceType type) {
        if (SequenceType.Tierce.equals(type)) {
            return context.getString(R.string.Tierce);
        }

        if (SequenceType.Quarte.equals(type)) {
            return context.getString(R.string.Quarte);
        }

        if (SequenceType.Quint.equals(type)) {
            return context.getString(R.string.Quint);
        }
        return context.getString(R.string.Sequence);
    }
    
    /**
     * Displays a message.
     * @param player which call the message function.
     * @param card played by player.
     */
    protected final void displayMessage(final Player player, final ArrayList<MessageData> messages) {
        final BooleanFlag flag = new BooleanFlag();
        handler.post(new Runnable() {
            public void run() {
                messageScreen = new MessageScreen(context, player, messages, flag);
                positionMessageScreen(messageScreen, player);
                messageScreen.show();
            }
        });

        while (flag.getValue()) {
            invalidateGame();
            sleep(PLAY_DELAY);
        }
    }
    
    /**
     * Sleeps for provided millisecond.
     * @param ms provided millisecond.
     */
    protected final void sleep(final long ms) {
        if (ms > 0) {
            try {
                Thread.sleep(ms);
            } catch (InterruptedException ex) {
                // D.N.
            }
        }
    }
    
    
    public final int getUpperCardY() {
        Canvas canvas = belotPanel.getBufferedCanvas();
        int dip4 = Belote.fromPixelToDip(context, 4);
        if (canvas != null) {
            Rectangle rect = belotPainter.getPlayerCardRectangle(canvas, beloteFacade, 0, beloteFacade.getHumanPlayer().getPartner());
            return rect.y + dip4;
        }
        
        return 0;
    }
    
    /**
     * Place message screen on position related to provided player.
     * @param messageScreen
     * @param player
     */
    private void positionMessageScreen(MessageScreen messageScreen, Player player) {
        Canvas canvas = belotPanel.getBufferedCanvas();

        if (canvas != null) {
            int buttonsHeigh = buttons.getVisibility() == View.VISIBLE ? buttons.getHeight() : 0;
            Rectangle rect = belotPainter.getPlayerCardRectangle(canvas, beloteFacade, 0, player);

            switch (player.getID()) {
            case 0:
                WindowManager.LayoutParams lp = messageScreen.getWindow().getAttributes();
                lp.gravity |= Gravity.TOP;
                lp.y = rect.y + rect.height;
                messageScreen.getWindow().setAttributes(lp);
                break;

            case 1:
                lp = messageScreen.getWindow().getAttributes();
                lp.gravity |= Gravity.LEFT;
                lp.x = rect.x + rect.width;
                lp.y = lp.y - buttonsHeigh / 2;
                messageScreen.getWindow().setAttributes(lp);
                break;

            case 2:
                lp = messageScreen.getWindow().getAttributes();
                lp.gravity |= Gravity.BOTTOM;
                lp.y = belotPanel.getHeight() + buttonsHeigh - rect.y;
                messageScreen.getWindow().setAttributes(lp);
                break;

            case 3:
                messageScreen.getWindow().setGravity(Gravity.CENTER_VERTICAL);
                lp = messageScreen.getWindow().getAttributes();
                lp.gravity |= Gravity.RIGHT;
                lp.x = belotPanel.getWidth() - rect.x;
                lp.y = lp.y - buttonsHeigh / 2;
                messageScreen.getWindow().setAttributes(lp);
                break;
            }
        }
    }
    
    /**
     * New announce deal.
     * @param repaint
     */
    protected final void newAnnounceDealRound() {
        if (beloteFacade.getGame().getAnnounceList().getContractAnnounce() != null) {
            beloteFacade.processTrickData();
        }
        beloteFacade.setNextDealAttackPlayer();
        beloteFacade.newGame();
        beloteFacade.getHumanPlayer().setSelectedCard(null);

        if (context.getWindow() != null && context.getWindow().isActive()) {
            invalidateGame(STANDARD_CARD_DELAY);
        } else {
            invalidateGame();
        }
    }
    
    /**
     * On surface change (reposition message screen if is visible)
     */
    public final void onSurfaceChanged() {
        if (messageScreen != null && messageScreen.isShowing()) {
            positionMessageScreen(messageScreen, messageScreen.getPlayer());
        }
    }
    
    /**
     * Called when end game activity is closed.
     */
    public final void onCloseEndGame() {
        //save game log
        //BeloteLog.saveGameInfo(beloteFacade.getGame(), context);
        
        sleep(PLAY_DELAY);
        newAnnounceDealRound();
    }
}
