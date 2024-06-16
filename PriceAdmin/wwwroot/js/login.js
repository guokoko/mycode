$('.tab a').on('click', function (e) {    
    e.preventDefault();
    $('.login-container').find('.tab.active a').css("border-bottom","solid 2px lightgray");
    $(this).parent().addClass('active');
    $(this).parent().siblings().removeClass('active');
       
    target = $(this).attr('href');

    $('.tab-content > div').not(target).hide();
    $(this).css("border-bottom","solid 2px #547ed6");
    $(target).fadeIn(600);
});