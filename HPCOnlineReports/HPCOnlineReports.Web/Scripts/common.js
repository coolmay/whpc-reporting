// ajax function
function AjaxCall(type, async, apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid) {
    if (isNullOrEmpty(apiurl)) {
        return;
    }
    if (isNullOrEmpty(apidata)) {
        apidata = "";
    }

    $.ajax({
        type: type,
        url: apiurl,
        data: apidata,
        dataType: "json",
        async: async,
        success: function (data) {
            try {
                if (!isNullOrEmpty(successCallback)) {
                    successCallback(data);
                }
            }
            catch (ex) {
                showError(ex, msgboxid);
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            try {
                var code = isNullOrEmpty(XMLHttpRequest.responseJSON) ? -1 : XMLHttpRequest.responseJSON.ActionResultCode;
                var msg = isNullOrEmpty(XMLHttpRequest.responseJSON) ? "No message." : XMLHttpRequest.responseJSON.Message;
                showAjaxError(textStatus, errorThrown, code, msg, msgboxid);
                if (!isNullOrEmpty(errorCallback)) {
                    errorCallback(XMLHttpRequest, textStatus, errorThrown);
                }
            }
            catch (ex) {
                showError(ex, msgboxid);
            }
        }
    }).always(function () {
        try {
            if (!isNullOrEmpty(alwaysCallback)) {
                alwaysCallback();
            }
        }
        catch (ex) {
            showError(ex, msgboxid);
        }
    });
}

function AjaxGetAsync(apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid) {
    AjaxCall("GET", true, apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid);
}

function AjaxGetSync(apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid) {
    AjaxCall("GET", false, apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid);
}

function AjaxPostAsync(apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid) {
    AjaxCall("POST", true, apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid);
}

function AjaxPostSync(apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid) {
    AjaxCall("POST", false, apiurl, apidata, successCallback, errorCallback, alwaysCallback, msgboxid);
}

function showAjaxError(status, error, code, msg, msgboxid) {
    var errormsg = "[" + status + "]" + " " + error + ": " + (isNullOrEmpty(code) ? "" : code) + " " + msg;
    showError(errormsg, msgboxid);
}


// common function
function showMessage(msg, msgboxid) {
    if (isNullOrEmpty(msgboxid)) {
        $("#message_box").html(msg);
    } else {
        $("#" + msgboxid).html(msg);
    }
}

function showError(msg, msgboxid) {
    var msghtml = "<div class='alert alert-dismissable alert-warning'><button class='close' type='button' data-dismiss='alert'>×</button><p>" + msg + "</p></div>";
    $("#alert_box").append(msghtml);

    if (isNullOrEmpty(msgboxid)) {
        $("#message_box").html(msg);
    } else {
        $("#" + msgboxid).html(msg);
    }
}

function showHelp(msg) {
    var msghtml = "<div class='alert alert-dismissable alert-info'><button class='close' type='button' data-dismiss='alert'>×</button><p>" + msg + "</p></div>";
    $("#help_box").html(msghtml);
}

function AddLoadingMark(containerid) {
    var container = $("#" + containerid);
    if (container.length <= 0) {
        return;
    }

    container.empty();
    container.append($("<div style='text-align: center;'><span class='loading-mark'></span> Loading...</div>"));
}

function scrollTo(itemid) {
    var scroll_offset = $("#" + itemid).offset();
    $("body,html").animate({
        scrollTop: scroll_offset.top - 60
    }, "slow");
}

function isNullOrEmpty(strVal) {
    if (strVal == null || strVal == undefined || strVal == '') {
        return true;
    } else {
        return false;
    }
}

function txt2Html(code) {
    code = code.replace(/&/mg, '&#38;');
    code = code.replace(/</mg, '&#60;');
    code = code.replace(/>/mg, '&#62;');
    code = code.replace(/\"/mg, '&#34;');
    code = code.replace(/\t/g, '  ');
    code = code.replace(/\r?\n/g, '<br/>');
    code = code.replace(/ /g, '&nbsp;');

    return code;
}

function html2Txt(code) {
    code = code.replace('&nbsp;', ' ');
    code = code.replace('<br/>', '\n');
    code = code.replace('<br>', '\n');
    code = code.replace('</br>', '');
    code = code.replace('&#34;', '"');
    code = code.replace('&#62;', '>');
    code = code.replace('&#60;', '<');
    code = code.replace('&#38;', '&');

    return code;
}

function dateFormat(datestring) {
    var date = new Date(datestring);
    var format = date.toUTCString();
    return format;
}

function time2Now(datestring) {
    var date = new Date(datestring);
    var now = new Date();
    var diff = (now - date) / 1000;

    var sec = 1;
    var min = 60 * sec;
    var hour = 60 * min;
    var day = 24 * hour;

    if (diff / day > 1) {
        return date.toDateString();
    }
    else if (diff / hour > 2) {
        return Math.floor(diff / hour) + " hrs";
    }
    else if (diff / hour > 1) {
        return "1 hr";
    }
    else if (diff / min > 2) {
        return Math.floor(diff / min) + " mins";
    }
    else if (diff / min > 1) {
        return "1 min";
    }
        //else if (diff / sec > 1) {
        //    return Math.floor(diff / sec) + "secs";
        //}
    else {
        return "Just now";
    }
}

function usernameFormat(usernamestring) {
    var str = usernamestring.replace(/\\/g, '\\\\');
    return str;
}

function moneyFormat(number, places, symbol, thousand, decimal) {
    number = number || 0;
    places = !isNaN(places = Math.abs(places)) ? places : 2;
    symbol = symbol !== undefined ? symbol : "$";
    thousand = thousand || ",";
    decimal = decimal || ".";
    var negative = number < 0 ? "-" : "",
        i = parseInt(number = Math.abs(+number || 0).toFixed(places), 10) + "",
        j = (j = i.length) > 3 ? j % 3 : 0;
    return symbol + negative + (j ? i.substr(0, j) + thousand : "") + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thousand) + (places ? decimal + Math.abs(number - i).toFixed(places).slice(2) : "");
}

function numberFormat(number, places) {
    number = number || 0;
    places = !isNaN(places = Math.abs(places)) ? places : 2;

    var n = parseFloat(number);
    return n.toFixed(places);
}