package com.karamanov.beloteGame.gui.screen.main.dealer;

import android.view.Gravity;
import android.view.View;
import android.view.WindowManager;
import belote.bean.GameMode;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.gui.screen.main.BeloteView;
import com.karamanov.beloteGame.gui.screen.main.announce.AnnounceDialog;
import com.karamanov.framework.MessageActivity;

/**
 * AnnounceDealer class.
 * @author Dimitar Karamanov
 */
final class AnnounceDealer extends BaseDealer {
    
    /**
     * Announce dialog.
     */
    private final AnnounceDialog announceDialog;

    /**
     * Constructor
     * @param context
     * @param belotPanel
     * @param buttons
     */
    public AnnounceDealer(MessageActivity context, BeloteView belotPanel, View buttons) {
        super(context, belotPanel, buttons);
        
        announceDialog = new AnnounceDialog(context, beloteFacade);
    }

    /**
     * Checks key click.
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    @Override
    public void checkKeyPressed(int keyCode) {
        processAnnounceDealRound();
    }
    
    /**
     * Checks pointer click.
     * @param x position.
     * @param y position.
     */
    @Override
    public void checkPointerPressed(float x, float y) {
        processAnnounceDealRound();
    }
    
    /**
     * Process announce deal round (Until 3 or 4 last pass announces).
     */
    private void processAnnounceDealRound() {
        if (beloteFacade.canDeal()) {
            processSingleAnnounceDeal();
        } else {
            if (beloteFacade.getGame().getAnnounceList().getContractAnnounce() == null) {
                newAnnounceDealRound();
            } else {
                newGame();
            }
        }
    }
    
    /**
     * Process single announce deal.
     */
    private void processSingleAnnounceDeal() {
        if (beloteFacade.getNextAnnouncePlayer().equals(beloteFacade.getHumanPlayer())) {
            showAnnounceDialog();
        } else {
            beloteFacade.processNextAnnounce();
        }

        invalidateGame();
    }
    
    /**
     * New game.
     */
    private void newGame() {
        beloteFacade.setGameMode(GameMode.PlayGameMode);
        beloteFacade.manageRestCards();
        invalidateGame();
    }
    
    /**
     * Shows announce panel.
     */
    private void showAnnounceDialog() {
        beloteFacade.setPlayerIsAnnouncing(true);
        invalidateGame();
        
        handler.post(new Runnable() {
            public void run() {
                announceDialog.init();
                WindowManager.LayoutParams layoutParams = announceDialog.getWindow().getAttributes();
                layoutParams.gravity = Gravity.BOTTOM;
                layoutParams.y = belotPainter.getFontHeight() + belotPainter.getCardHeight();
                if (buttons.getVisibility() == View.VISIBLE) {
                    layoutParams.y = layoutParams.y + buttons.getHeight();
                }
                announceDialog.getWindow().setAttributes(layoutParams);
                announceDialog.show();
            }
        });

        Belote belote = (Belote) context.getApplication();
        belote.getMessageProcessor().stopMessaging();
    }
}
