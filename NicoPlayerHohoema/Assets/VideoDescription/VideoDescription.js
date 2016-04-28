
for (var i = 0; i < document.links.length; i++)
{
    document.links[i].onclick = function ()
    {
        // ホストコントロールにリンクURLを通知
        window.external.notify(this.href);

        // リンクのデフォルト動作をキャンセル
        return false;
    }
}
